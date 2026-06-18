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
using System;
using System.Collections.Generic;
using System.Linq;

namespace HumanOS.IoT.Designer.Library.Scripts;

/// <summary>
/// Processing script for ToolLoadedEvent processor
/// </summary>
public class TToolLoadedEvent : TAbstractProcessingScriptObject
{
  ///<see cref="TAbstractProcessingScriptObject"/>
  public override void process(IProcessingNode Processor)
  {
    Logger.writeInfo($"ToolLoadedEvent processor started...");

    // Create ToolIdentifier entity ----------------------------------------------------
    TGenericEntity EntityToolIdentifier = createToolIdentifier(
        Guid.NewGuid(),
        "NameToolIdent", /*Processor.getProperty<string>("InputName")*/
        123, /*Processor.getProperty<int>("InputDuplonumber")*/
        "123Unique" /*Processor.getProperty<string>("InputUniqueId")*/
    );

    // Create ToolLoadedEvent entity ----------------------------------------------------
    TGenericEntity EntityToolLoadedEvent = createToolLoadedEvent(
        Guid.NewGuid(),
        "123Id", /*Processor.getProperty<string>("InputName")*/
        EntityToolIdentifier,
        10,
        20
    );

    // Output ----------------------------------------------------
    Processor.setProperty("Output", EntityToolLoadedEvent);
    Guid DeviceId = Processor.getProperty<Guid>("DeviceId");
    Logger.writeInfo($"ToolLoadedEvent tool used: {EntityToolLoadedEvent})");
    Logger.writeInfo("ToolLoadedEvent finished...");
  }

  /// <summary>
  /// Calculates the Percentage value
  /// </summary>
  private double calculatePercent(double fOrig, double fMin, double fValue)
  {
    double fRange = fOrig - fMin;

    // Prevent division by zero or negative ranges
    if (fRange <= 0)
    {
      fRange = 0;
      Logger.writeInfo($"Invalid range (fOrig <= fMin). fOrig={fOrig}, fMin={fMin}. Returning 0.");
    }

    return fRange <= 0 ? 0 : 100.0 * (fValue - fMin) / (fOrig - fMin);
  }

  /// <summary>
  /// Creates the ToolIdentifierType
  /// </summary>
  private TGenericEntity createToolIdentifier(Guid Id, string strName, int iDuplonumber, string strUniqueId)
  {
    List<TEntityField> lstFieldsToolIdentifier = new List<TEntityField>
    {
      new TEntityField("Id",          typeof(Guid),   true,  false),
      new TEntityField("Name",        typeof(string), false, false),
      new TEntityField("Duplonumber", typeof(int),    false, false),
      new TEntityField("UniqueId",    typeof(string), false, false)
    };

    TGenericEntity EntityToolIdentifier = new TGenericEntity(lstFieldsToolIdentifier, Id);

    EntityToolIdentifier.setValue("Name", strName);
    EntityToolIdentifier.setValue("Duplonumber", iDuplonumber);
    EntityToolIdentifier.setValue("UniqueId", strUniqueId);

    return EntityToolIdentifier;
  }

  /// <summary>
  /// Creates the ToolLoadedEventType
  /// </summary>
  private TGenericEntity createToolLoadedEvent(Guid Id, string strFacilityComponentId, TGenericEntity EntityIdentifier, int iUsageOffset, int iToolLifeConsumption)
  {
    List<TEntityField> lstFieldsToolLoadedEvent = new List<TEntityField>
    {
      // ToolLoadedEvent
      //new TEntityField("DestinationToolPositionId",  typeof(string), false, false),
      new TEntityField("LoadedBy",                   typeof(int),    false, false),

      // ToolMovedEvent
      new TEntityField("SourceToolPositionId",      typeof(string), false, false),
      new TEntityField("DestinationToolPositionId", typeof(string), false, false),

      // ToolEvent
      new TEntityField("Id",                  typeof(Guid),           true,  false),
      new TEntityField("FacilityComponentId", typeof(string),         false, false),
      new TEntityField("Identifier",          typeof(TGenericEntity), false, false),        

      // BaseEventType
      new TEntityField("EventId",     typeof(string),   false, false),
      new TEntityField("EventType",   typeof(string),   false, false),
      new TEntityField("SourceNode",  typeof(string),   false, false),
      new TEntityField("Time",        typeof(DateTime), false, false),
      new TEntityField("RecieveTime", typeof(DateTime), false, false),
      new TEntityField("Message",     typeof(string),   false, false),
      new TEntityField("Severity",    typeof(short),    false, false)
    };

    TGenericEntity EntityToolLoadedEvent = new TGenericEntity(lstFieldsToolLoadedEvent, Id);

    // TooLoadedEvent values
    EntityToolLoadedEvent.setValue("DestinationToolPositionId", "Dest123");
    EntityToolLoadedEvent.setValue("LoadedBy", 3);

    // ToolMovedEvent values
    //EntityToolLoadedEvent.setValue("DestinationToolPositionId", "Dest123");
    EntityToolLoadedEvent.setValue("SourceToolPositionId", "Source123");

    // ToolEvent values
    EntityToolLoadedEvent.setValue("FacilityComponentId", strFacilityComponentId);
    EntityToolLoadedEvent.setValue("Identifier", EntityIdentifier);

    // BaseEventType values
    DateTime dtNow = TDateTime.UtcNowHighRes;
    EntityToolLoadedEvent.setValue("EventId", "EventId123");
    EntityToolLoadedEvent.setValue("EventType", "ToolLoadedEventType");
    EntityToolLoadedEvent.setValue("SourceNode", "ProductionManagement");
    EntityToolLoadedEvent.setValue("Time", dtNow);
    EntityToolLoadedEvent.setValue("RecieveTime", dtNow);
    EntityToolLoadedEvent.setValue("Message", "The ToolLoadedEventType has been fired.");
    EntityToolLoadedEvent.setValue("Severity", (short)500);

    return EntityToolLoadedEvent;
  }
}
