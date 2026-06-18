/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2024 – www.cybertech.swiss         *
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
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;


namespace HumanOS.IoT.Designer.Library.Scripts;

/// <summary>
/// Processing script for AutomationStateChangedEvent processor
/// </summary>
public class TAutomationStateChangedEvent : TAbstractProcessingScriptObject
{
  ///<see cref="TAbstractProcessingScriptObject"/>
  public override void process(IProcessingNode Processor)
  {
    Logger.writeInfo($"AutomationStateChangedEvent processor started...");

    // Read Inputs
    int iTrigger = Processor.getProperty<int>("InputTrigger");
    bool bRemoteMode = Processor.getProperty<bool>("InputRemoteMode");
    int iOperationState = Processor.getProperty<int>("InputOperationState");
    int iServiceRequest = Processor.getProperty<int>("InputServiceRequest");

    Logger.writeInfo(
      $"Readed Configuration:\n" +
      $"  iTrigger:        {iTrigger}\n" +
      $"  bRemoteMode:     {bRemoteMode}\n" +
      $"  iOperationState: {iOperationState}\n" +
      $"  iServiceRequest: {iServiceRequest}"
    );

    // Create AutomationStateChangedEvent entity
    List<TEntityField> lstFieldsAutomationStateChangedEvent = new List<TEntityField>
    {
      // AutomationStateChangedEvent
      new TEntityField("Id",                 typeof(Guid),   true,  false),
      new TEntityField("FacilityComponentId",typeof(string), false, false),
      new TEntityField("RemoteMode",         typeof(bool),   false, false),
      new TEntityField("OperationState",     typeof(int),    false, false),
      new TEntityField("ServiceRequest",     typeof(int),    false, false),

      // BaseEventType
      new TEntityField("EventId",     typeof(string),   false, false),
      new TEntityField("EventType",   typeof(string),   false, false),
      new TEntityField("SourceNode",  typeof(string),   false, false),
      new TEntityField("Time",        typeof(DateTime), false, false),
      new TEntityField("ReceiveTime", typeof(DateTime), false, false),
      new TEntityField("Message",     typeof(string),   false, false),
      new TEntityField("Severity",    typeof(short),    false, false)
    };

    TGenericEntity EntityAutomationStateChangedEvent = new TGenericEntity(lstFieldsAutomationStateChangedEvent, Guid.NewGuid());

    // ToolEvent values
    Guid DeviceId = Processor.getProperty<Guid>("DeviceId");
    EntityAutomationStateChangedEvent.setValue("FacilityComponentId", $"{DeviceId}");
    EntityAutomationStateChangedEvent.setValue("RemoteMode", bRemoteMode);
    EntityAutomationStateChangedEvent.setValue("OperationState", iOperationState);
    EntityAutomationStateChangedEvent.setValue("ServiceRequest", iServiceRequest);

    // BaseEventType values
    DateTime dtNow = TDateTime.UtcNowHighRes;
    EntityAutomationStateChangedEvent.setValue("EventId", "EventId123");
    EntityAutomationStateChangedEvent.setValue("EventType", "AutomationStateChangedEventType");
    EntityAutomationStateChangedEvent.setValue("SourceNode", "AutomationState");
    EntityAutomationStateChangedEvent.setValue("Time", dtNow);
    EntityAutomationStateChangedEvent.setValue("ReceiveTime", dtNow);
    EntityAutomationStateChangedEvent.setValue("Message", "The AutomationStateChangedEventType has been fired.");
    EntityAutomationStateChangedEvent.setValue("Severity", (short)500);

    Logger.writeInfo($"Printed Entity: {printEntity(EntityAutomationStateChangedEvent)}");

    // Output
    Processor.setProperty("Output", EntityAutomationStateChangedEvent);
    Logger.writeInfo("AutomationStateChangedEvent finished...");
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
