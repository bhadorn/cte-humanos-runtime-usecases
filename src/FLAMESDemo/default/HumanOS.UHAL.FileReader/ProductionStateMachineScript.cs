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

using CyberTech;
using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Entity;
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.InfoModel.StateMachine;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Processing.StateMachine;
using HumanOS.Kernel.UHAL.InfoModel;
using HumanOS.Kernel.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.Kernel.Test.TestFiles.Scripts.Processing;

/// <summary>
/// Handles the Production state machine
/// </summary>
public class TProductionStateMachine : TAbstractStateMachineScript
{
  #region Implementation of TAbstractStateMachineScript

  ///<see cref="TAbstractStateMachineScript"/>
  public override async Task onEnterStateAsync(TTriggerEventInfo Trigger, TStateInfo Source, TStateInfo Destination, TStateMachineContext Context)
  {
    Logger.writeInfo($"TProductionStateMachine: State '{Destination.Name}' entered.");
    processEvent(Context.ProcessingNode, Source.Name, Destination.Name);
    await Task.CompletedTask.ConfigureAwait(false);
  }

  ///<see cref="TAbstractStateMachineScript"/>
  public override async Task onExitStateAsync(TTriggerEventInfo Trigger, TStateInfo Source, TStateInfo Destination, TStateMachineContext Context)
  {
    Logger.writeInfo($"TProductionStateMachine: State '{Source.Name}' exit.");
    await Task.CompletedTask.ConfigureAwait(false);
  }

  ///<see cref="TAbstractStateMachineScript"/>
  public override async Task onInternalTransitionAsync(TTriggerEventInfo Trigger, TStateInfo Source, TStateMachineContext Context)
  {
    Logger.writeInfo($"TProductionStateMachine: Internal transition from state '{Source.Name}'.");
    await Task.CompletedTask.ConfigureAwait(false);
  }

  ///<see cref="IStateMachineLogic"/>
  public override async Task<bool> processAsync(IProcessingNode Processor, TStateInfo State, TStateMachineContext Context, CancellationToken Token)
  {
    Token.ThrowIfCancellationRequested();
    try
    {
      #region --- Read property ---

      //Example: Reading a typed property from the processor configuration
      /*
      int iInput = Processor.getProperty<int>("input");
      Logger.writeInfo(
          $"Readed Configuration:\n" +
          $"  iInput: {iInput}"
        );
      */
      #endregion --- Read property ---

      #region --- Create and publish a custom entity ---

      // NOTE:
      // - return TRUE if you create a custom entity
      // - return FALSE to let the framework create the default entity
      /*
      List<TEntityField> lstFields = new List<TEntityField>();
      lstFields.Add(new TEntityField("Id", typeof(Guid), true, false));
      lstFields.Add(new TEntityField("TimeStamp", typeof(DateTime), false, false));
      lstFields.Add(new TEntityField("State", typeof(String), false, false));
      lstFields.Add(new TEntityField("Data", typeof(TGenericEntity), false, false));

      TGenericEntity Entity = new TGenericEntity(lstFields, Guid.NewGuid());
      Entity.setValue("TimeStamp", TDateTime.UtcNowHighRes);
      Entity.setValue("State", State);
      Entity.setValue("Data", Context.Data);

      Processor.setProperty<TGenericEntity>("State", Entity);
      */
      #endregion --- Create and publish a custom entity ---

      #region --- Call command ---

      // Example: Calling a command on a controller group
      /*
      IGroupRelation nDeviceNode = Processor.NodeSpace.queryNodeLocally(n => n.Name == "Controller") as IGroupRelation;
      if (nDeviceNode != null)
      {
        TCommandArgs Args = new TCommandArgs();
        TCommandResult Result = TCommandHelper.call(nDeviceNode, "CommandName", Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Command 'CommandName' failed. {Result.ErrorMessage}");
        }
      }
      else
      {
        throw new ArgumentException($"No Group 'Controller' not found.");
      }
      */
      #endregion --- Call command ---

      #region --- Write value on an existing data node ---

      // Example: Updating a string data node
      /*
      IGroupRelation nController = Processor.NodeSpace.queryNodeLocally(n => n.Name == "Controller") as IGroupRelation;
      if (nController == null)
      {
        throw new ArgumentException($"Could not find Controller group node.");
      }
      IDataNode<string> QueryNode = nController.queryNode(n => n.Name == "QueryNodeName") as IDataNode<string>;
      QueryNode.passValue("success");
      */
      #endregion --- Write value on an existing data node ---
    }
    catch (OperationCanceledException)
    {
      Logger.writeWarning("Processing was cancelled.");
      throw;
    }
    catch (Exception ex)
    {
      Logger.writeError($"Processing failed: {ex.Message}");
      throw;
    }

    return await Task.FromResult(false).ConfigureAwait(false); // Use TRUE, if you have a custom entity
  }

  #endregion Implementation of TAbstractStateMachineScript


  private void processEvent(IProcessingNode Processor, string strSourceName, string strDestinationName)
  {
    Logger.writeInfo($"ProductionPrograTransitionEvent processor started...");

    // Read Inputs ----------------------------------------------------
    int iTrigger = Processor.getProperty<int>("Event_ValueIn");
    string strIdentifier = Processor.getProperty<string>("ProgramIdentifierIn");
    string strLastInformationIn = Processor.getProperty<string>("LastInformationIn");
    string strLastProgramBlockIn = Processor.getProperty<string>("LastProgramBlockIn");
    string strLastToolIn = Processor.getProperty<string>("LastToolIn");
    bool bQualityWithinTolerancesIn = Processor.getProperty<bool>("QualityWithinTolerancesIn");
    uint bReasonForAbortionIn = Processor.getProperty<uint>("ReasonForAbortionIn");

    Logger.writeInfo(
      $"Readed Configuration:\n" +
      $"  iTrigger:                   {iTrigger}\n" +
      $"  strIdentifier:              {strIdentifier}\n" +
      $"  strLastInformationIn:       {strLastInformationIn}\n" +
      $"  strLastProgramBlockIn:      {strLastProgramBlockIn}\n" +
      $"  strLastToolIn:              {strLastToolIn}\n" +
      $"  bQualityWithinTolerancesIn: {bQualityWithinTolerancesIn}\n" +
      $"  bReasonForAbortionIn:       {bReasonForAbortionIn}"
    );

    // Create ProductionProgramTransitionEvent entity ----------------------------------------------------
    List<TEntityField> lstFieldsProductionProgramTransitionEvent = new List<TEntityField>
      {
        // ProductionProgramTransitionEventType
        new TEntityField("Id",                      typeof(Guid),   true,  false),
        new TEntityField("FacilityComponentId",     typeof(string), false, false),
        new TEntityField("Identifier",              typeof(string), false, false),
        new TEntityField("LastProgramBlock",        typeof(string), false, false),
        new TEntityField("LastTool",                typeof(string), false, false),
        new TEntityField("LastInformation",         typeof(string), false, false),
        new TEntityField("QualityWithinTolerances", typeof(bool),   false, false),
        new TEntityField("ReasonForAbortion",       typeof(uint),   false, false),
        
        // TransitionEventType values
        new TEntityField("Transition", typeof(TLocalizedText),   false, false),
        new TEntityField("FromState",  typeof(TLocalizedText),   false, false),
        new TEntityField("ToState",    typeof(TLocalizedText),   false, false),

        // BaseEventType values
        new TEntityField("EventId",     typeof(string),   false, false),
        new TEntityField("EventType",   typeof(string),   false, false),
        new TEntityField("SourceNode",  typeof(string),   false, false),
        new TEntityField("Time",        typeof(DateTime), false, false),
        new TEntityField("RecieveTime", typeof(DateTime), false, false),
        new TEntityField("Message",     typeof(string),   false, false),
        new TEntityField("Severity",    typeof(short),    false, false)
      };

    TGenericEntity EntityProductionProgramTransitionEvent = new TGenericEntity(lstFieldsProductionProgramTransitionEvent, Guid.NewGuid());

    // ToolEvent values
    Guid DeviceId = Processor.getProperty<Guid>("DeviceId");
    EntityProductionProgramTransitionEvent.setValue("FacilityComponentId", $"{DeviceId}");
    EntityProductionProgramTransitionEvent.setValue("Identifier", strIdentifier);
    EntityProductionProgramTransitionEvent.setValue("LastProgramBlock", strLastProgramBlockIn);
    EntityProductionProgramTransitionEvent.setValue("LastTool", strLastToolIn);
    EntityProductionProgramTransitionEvent.setValue("LastInformation", strLastInformationIn);
    EntityProductionProgramTransitionEvent.setValue("QualityWithinTolerances", bQualityWithinTolerancesIn);
    EntityProductionProgramTransitionEvent.setValue("ReasonForAbortion", bReasonForAbortionIn);

    // TransitionEventType values
    EntityProductionProgramTransitionEvent.setValue("Transition", new TLocalizedText("TransitionString"));
    EntityProductionProgramTransitionEvent.setValue("FromState", new TLocalizedText(strSourceName));
    EntityProductionProgramTransitionEvent.setValue("ToState", new TLocalizedText(strDestinationName));

    // BaseEventType values
    DateTime dtNow = TDateTime.UtcNowHighRes;
    EntityProductionProgramTransitionEvent.setValue("EventId", "EventId123");
    EntityProductionProgramTransitionEvent.setValue("EventType", "ProductionProgramTransitionEventType");
    EntityProductionProgramTransitionEvent.setValue("SourceNode", "ProductionManagement");
    EntityProductionProgramTransitionEvent.setValue("Time", dtNow);
    EntityProductionProgramTransitionEvent.setValue("RecieveTime", dtNow);
    EntityProductionProgramTransitionEvent.setValue("Message", "The ProductionProgramTransitionEventType has been fired.");
    EntityProductionProgramTransitionEvent.setValue("Severity", (short)500);

    Logger.writeInfo($"Printed Entity: {printEntity(EntityProductionProgramTransitionEvent)}");

    // Output ----------------------------------------------------
    Processor.setProperty("Output", EntityProductionProgramTransitionEvent);


    // Set State
    try
    {
      IGroupRelation? nFlames = Processor.NodeSpace.queryNodeLocally(n => n.Name == "FLAMES") as IGroupRelation ?? throw new ArgumentException($"Could not find 'Controller' group node.");
      IGroupRelation? nMachine = nFlames.queryNodeLocally(n => n.Name == "Machine") as IGroupRelation ?? throw new ArgumentException($"Could not find 'Machine' group node.");
      IGroupRelation? nProductionManagement = nMachine.queryNodeLocally(n => n.Name == "ProductionManagement") as IGroupRelation ?? throw new ArgumentException($"Could not find 'ProductionManagement' group node.");
      IGroupRelation? nProductionProgram = nProductionManagement.queryNodeLocally(n => n.Name == "ProductionProgram") as IGroupRelation ?? throw new ArgumentException($"Could not find 'ProductionProgram' group node.");
      IGroupRelation? nState = nProductionProgram.queryNodeLocally(n => n.Name == "State") as IGroupRelation ?? throw new ArgumentException($"Could not find 'State' group node.");

      IDataNode<TLocalizedText>? QueryNode = nState.queryNode(n => n.Name == "CurrentState") as IDataNode<TLocalizedText> ?? throw new ArgumentException($"Could not find 'CurrentState' data node.");

      string strCurrentState = "unknown state";
      switch (iTrigger)
      {
        case 0:
          strCurrentState = "Initialized";
          break;
        case 1:
          strCurrentState = "Running";
          break;
        case 2:
          strCurrentState = "Stopped";
          break;
        case 3:
          strCurrentState = "Aborted";
          break;
        case 4:
          strCurrentState = "Interrupted";
          break;
        default:
          break;
      }

      QueryNode?.passValue(TValueConverter.convertToObject<TLocalizedText>(strCurrentState), false);
    }
    catch (OperationCanceledException)
    {
      Logger.writeWarning("Processing was cancelled.");
      throw;
    }
    catch (Exception ex)
    {
      Logger.writeError($"Processing failed: {ex.Message}");
      throw;
    }

    Logger.writeInfo("ProductionProgramTransitionEventType finished...");
  }

  /// <summary>
  /// Print single entity with its properties.
  /// </summary>
  private string printEntity(TGenericEntity nEntity)
  {
    string strReturn = "";
    if (nEntity != null)
    {
      JObject jData = new JObject();
      foreach (KeyValuePair<string, object> FieldValue in nEntity.getFieldValues())
      {
        string strValue = "";
        if (FieldValue.Value != null)
        {
          strValue = TValueConverter.convertToString(FieldValue.Value);
        }
        jData.Add(FieldValue.Key, strValue);
      }
      strReturn = jData.ToString();
    }
    else
    {
      Logger.writeError($"Entity is null or empty.");
    }

    return strReturn;
  }
}
