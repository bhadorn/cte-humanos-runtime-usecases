using CyberTech.Diagnostics;
using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Entity;
using HumanOS.Kernel.DataModel.Rules;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  // Called every 10 s by the timer in the DataModel object "Polling Timer" (rule "OnTimer").
  //
  // Sequence per tick:
  //   1. Read the active production orders via the SQL device (command "ReadEntities" = Sql.ReadEntities).
  //   2. Compare them with the state of the previous tick.
  //        newly added -> data node "PoStart"  (value = order number)
  //        disappeared -> data node "PoFinish" (value = order number)
  //   3. The rest is done by the publisher "PoPublisher" of HumanOS.PeSeL.MQTTClient: it is event-based
  //      (SamplingRate 0) and publishes every value written to one of the two data nodes to
  //      <Topic>/PoStart or <Topic>/PoFinish (see PoPayload.cs). The certificate authentication is
  //      configured there as well.
  //
  // The SQL device is looked up by the property "Role". The data nodes "PoStart" and "PoFinish"
  // belong to this device (DeviceTemplates/MES MySQL.json) and are looked up by name + DeviceId.
  // They have no address, so the SqlClient driver never polls them itself.
  //
  // The comparison state is static because the runtime may create a new script instance per tick.
  public class TProductionOrderMonitor : TAbstractRuleScriptObject
  {
    // EntityType "active_order" from DeviceTemplates/MES MySQL.json (one column: PoNumber, Int32).
    private static readonly Guid ActiveOrderTypeId = new Guid("07b56f22-21eb-46b5-b5c9-066abe08710e");

    private const string MES_DB_ROLE = "MesDB";

    // Data nodes of the SQL device that the publisher "PoPublisher" publishes to MQTT.
    private const string PO_START_NODE  = "PoStart";
    private const string PO_FINISH_NODE = "PoFinish";

    // TODO: adapt to the real table. Important: the result column MUST be named "PoNumber"
    // (name of the field in the EntityType), hence the "AS PoNumber".
    private const string ACTIVE_ORDERS_QUERY =
      "SELECT `PO_NR` AS PoNumber FROM `production_orders` WHERE `Status` = 'ACTIVE';";

    // The rows (entities) of the last successful ReadEntities.
    // null = no successful read since the runtime was started.
    private static IEnumerable<TGenericEntity> s_PreviousOrders;
    private static readonly object s_Lock = new object();

    ///<see cref="TAbstractRuleScriptObject"/>
    public override void execute(IKernelAccess Kernel, IVariablePool Pool)
    {
      // One tick at a time: if the DB is slow, two ticks must not overtake each other and
      // produce duplicate messages.
      lock (s_Lock)
      {
        CancellationToken Token = Kernel.TerminateToken;

        IGroupRelation              DbDevice;
        IDataNode<int>              PoStartNode;
        IDataNode<int>              PoFinishNode;
        IEnumerable<TGenericEntity> Current;
        try
        {
          DbDevice     = getDeviceAsync(Kernel, N => N.getProperty<string>("Role", null) == MES_DB_ROLE, Token, 5.0).GetAwaiter().GetResult();
          PoStartNode  = getDataNode(Kernel, DbDevice, PO_START_NODE, Token);
          PoFinishNode = getDataNode(Kernel, DbDevice, PO_FINISH_NODE, Token);
          Current      = readActiveOrders(DbDevice, Token);
        }
        catch (Exception Exc)
        {
          // Read error: do NOT touch the comparison state. Otherwise an empty list after a
          // DB outage would trigger a false PoFinish for every running order.
          TLogger.writeError($"ProductionOrderMonitor: reading active orders failed: {Exc.Message}");
          return;
        }

        // First successful read after startup: only remember the baseline, send nothing.
        // Otherwise every runtime restart would send a PoStart for all running orders.
        if (s_PreviousOrders == null)
        {
          s_PreviousOrders = Current;
          TLogger.writeInfo($"ProductionOrderMonitor: baseline with {Current.Count()} active order(s).");
          return;
        }

        // Compare by the key field PoNumber, not by the entity objects themselves
        // (every ReadEntities returns new objects).
        List<TGenericEntity> lstStarted  = Current.Where(Order => !containsPo(s_PreviousOrders, getPo(Order))).ToList();
        List<TGenericEntity> lstFinished = s_PreviousOrders.Where(Order => !containsPo(Current, getPo(Order))).ToList();

        // Every write raises its own event (TriggerType "OnEvent") and therefore its own
        // MQTT message, even if several orders start or end in the same tick.
        foreach (TGenericEntity Order in lstStarted)
        {
          PoStartNode.passValue(getPo(Order), false, EDataState.Good);
          TLogger.writeInfo($"ProductionOrderMonitor: PoStart = {getPo(Order)}");
        }

        foreach (TGenericEntity Order in lstFinished)
        {
          PoFinishNode.passValue(getPo(Order), false, EDataState.Good);
          TLogger.writeInfo($"ProductionOrderMonitor: PoFinish = {getPo(Order)}");
        }

        s_PreviousOrders = Current;
      }
    }

    private static int getPo(TGenericEntity Order)
    {
      return Order.getValue<int>("PoNumber");
    }

    private static bool containsPo(IEnumerable<TGenericEntity> Orders, int iPo)
    {
      return Orders.Any(Order => getPo(Order) == iPo);
    }

    // Looks up a data node of the SQL device by its name. The driver sets the property
    // "DeviceId" = id of the device (= GlobalId of the device group) on every data node.
    private static IDataNode<int> getDataNode(IKernelAccess Kernel, IGroupRelation Device, string strName, CancellationToken Token)
    {
      Guid DeviceId = Device.GlobalId;
      IDataNode<int> nNode = Kernel.NodeSpace.queryOrWaitForNodeLocallyAsync(N => N.Name == strName && N.getProperty<Guid>("DeviceId", Guid.Empty) == DeviceId,
                                                                            TimeSpan.FromSeconds(5.0), Token)
                                             .GetAwaiter().GetResult() as IDataNode<int>;
      if (nNode == null)
      {
        throw new InvalidOperationException($"Data node '{strName}' not found on device '{Device.Name}'.");
      }

      return nNode;
    }

    // Sql.ReadEntities: executes the SELECT; every row is returned as a TGenericEntity of the EntityType.
    private static IEnumerable<TGenericEntity> readActiveOrders(IGroupRelation DbDevice, CancellationToken Token)
    {
      TCommandArgs Args = new TCommandArgs();
      Args.Input["QueryCommand"]     = ACTIVE_ORDERS_QUERY;
      Args.Input["EntityTypeNodeId"] = ActiveOrderTypeId;

      TCommandResult Result = TCommandHelper.callAsync(DbDevice, "ReadEntities", Args, Token: Token).GetAwaiter().GetResult();
      if (Result.State != EProcessingState.Good)
      {
        throw new InvalidOperationException($"ReadEntities failed: {Result.ErrorMessage}");
      }

      return Args.getOutputArgument<TGenericEntity[]>("Data") ?? Array.Empty<TGenericEntity>();
    }
  }
}
