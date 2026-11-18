/*****************************************************************************
* Copyright (C) by CyberTech Engineering 2026 – www.cybertech.swiss         *
*****************************************************************************
* Project: HumanOS (R)
* Date   : 2026
*****************************************************************************
* License:                                                                  *
*   This library is protected software; you are not allowed to redistribute *
*   whole or part of it to other companies or external persons without the  *
*   authorization of the CEO CyberTech Engineering GmbH.                    *
*****************************************************************************/

using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Script;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Reads temperature and setpoint of the simulated machine and returns the machine state (Heating, Cooling or
  /// At setpoint) together with the deviation
  /// </summary>
  public class TGetMachineStateCommand : TAbstractCommandScriptObject2
  {
    /// <summary>
    /// Deviation in °C still reported as "At setpoint"
    /// </summary>
    private const double Tolerance = 0.5;

    ///<see cref="TAbstractCommandScriptObject2"/>
    public override async Task<TCommandResult> executeCommandAsync(ICommandCallContext CallContext, CancellationToken Token)
    {
      TCommandResult Retval = new TCommandResult();
      try
      {
        double fTemperature = readValue(CallContext, "Machine", "Temperature");
        double fSetpoint = readValue(CallContext, "Machine", "Setpoint");
        double fDeviation = Math.Round(fTemperature - fSetpoint, 1);

        string strState = "At setpoint";
        if (fDeviation < -Tolerance)
        {
          strState = "Heating";
        }
        else if (fDeviation > Tolerance)
        {
          strState = "Cooling";
        }
        CallContext.setOutputArgumentValue("State", strState);
        CallContext.setOutputArgumentValue("Deviation", fDeviation);
      }
      catch (Exception Exc)
      {
        Retval.setErrorInfo(EProcessingState.BadProcessing, Exc);
      }
      return await Task.FromResult(Retval).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the current value of a data node from the device
    /// </summary>
    /// <param name="CallContext">context of the command call</param>
    /// <param name="strDeviceName">name of the device</param>
    /// <param name="strNodeName">name of the data node</param>
    /// <returns>value of the data node</returns>
    /// <exception cref="InvalidOperationException">data node not found or without valid value</exception>
    private static double readValue(ICommandCallContext CallContext, string strDeviceName, string strNodeName)
    {
      INodeSpace NodeSpace = (INodeSpace)CallContext.CallingNode.Relations.First(n => n is INodeSpace);
      IGroupRelation nDevice = NodeSpace.queryNodesLocally(n => n.Name == strDeviceName).OfType<IGroupRelation>().FirstOrDefault();
      IDataNode<double> nDataNode = nDevice?.queryNodeLocally(n => n.Name == strNodeName) as IDataNode<double>;
      if (nDataNode == null)
      {
        throw new InvalidOperationException($"Data node '{strDeviceName}.{strNodeName}' not found.");
      }

      //bInvokeReadCommand = true: reads the current value from the JSON file
      EDataState eState = nDataNode.requestValue(out double fValue, true);
      if (eState != EDataState.Good)
      {
        throw new InvalidOperationException($"Data node '{strDeviceName}.{strNodeName}' has no valid value ({eState}).");
      }
      return fValue;
    }
  }
}
