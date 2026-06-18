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
/// Processing script for ToolUsedEvent processor
/// </summary>
public class TToolUsedEvent : TAbstractProcessingScriptObject
{
  ///<see cref="TAbstractProcessingScriptObject"/>
  public override void process(IProcessingNode Processor)
  {
    Logger.writeInfo($"ToolUsedEvent processor started...");

    int iToolUsed = Processor.getProperty<int>("InputToolUsedUpdated");
    if (iToolUsed > 0)
    {
      // Read Inputs ----------------------------------------------------      
      double fOrigDiam = Processor.getProperty<double>("InputOrigDiam");
      double fOrigWidth = Processor.getProperty<double>("InputOrigWidth");
      double fMinDiam = Processor.getProperty<double>("InputMinDiam");
      double fMinWidth = Processor.getProperty<double>("InputMinWidth");
      double fDiam = Processor.getProperty<double>("InputDiam");
      double fWidth = Processor.getProperty<double>("InputWidth");

      Logger.writeInfo(
        $"Readed Configuration:\n" +
        $"  fOrigDiam:  {fOrigDiam}\n" +
        $"  fOrigWidth: {fOrigWidth}\n" +
        $"  fMinDiam:   {fMinDiam}\n" +
        $"  fMinWidth:  {fMinWidth}\n" +
        $"  fDiam:      {fDiam}\n" +
        $"  fWidth:     {fWidth}"
      );

      double fDiameterPercent = calculatePercent(fOrigDiam, fMinDiam, fDiam);
      double fWidthPercent = calculatePercent(fOrigWidth, fMinWidth, fWidth);
      double fOutputPercent = Math.Min(fDiameterPercent, fWidthPercent);

      Logger.writeInfo($"ToolUsedEvent tool used: {fOutputPercent} " +
                     $"({fDiameterPercent};{fWidthPercent})");

      // Create ToolIdentifier entity ----------------------------------------------------
      TGenericEntity EntityToolIdentifier = createToolIdentifier(
          Guid.NewGuid(),
          "NameToolIdent", /*Processor.getProperty<string>("InputName")*/
          123, /*Processor.getProperty<int>("InputDuplonumber")*/
          "123Unique" /*Processor.getProperty<string>("InputUniqueId")*/
      );

      // Create ToolUsedEvent entity ----------------------------------------------------
      TGenericEntity EntityToolUsedEvent = createToolUsedEvent(
          Guid.NewGuid(),
          "123Id", /*Processor.getProperty<string>("InputName")*/
          EntityToolIdentifier,
          10,
          20
      );

      // Output ----------------------------------------------------
      Processor.setProperty("Output", EntityToolUsedEvent);
      Guid DeviceId = Processor.getProperty<Guid>("DeviceId");
      Logger.writeInfo($"ToolUsedEvent tool used: {EntityToolUsedEvent})");
    }
    Logger.writeInfo("ToolUsedEvent finished...");
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
  /// Creates the ToolUsedEventType
  /// </summary>
  private TGenericEntity createToolUsedEvent(Guid Id, string strFacilityComponentId, TGenericEntity EntityIdentifier, int iUsageOffset, int iToolLifeConsumption)
  {
    List<TEntityField> lstFieldsToolUsedEvent = new List<TEntityField>
    {
      // ToolEvent
      new TEntityField("Id",                 typeof(Guid),        true,  false),
      new TEntityField("FacilityComponentId",typeof(string),      false, false),
      new TEntityField("Identifier",         typeof(TGenericEntity), false, false),
      new TEntityField("UsageOffset",        typeof(int),         false, false),
      new TEntityField("ToolLifeConsumtion", typeof(int),         false, false),

      // BaseEventType
      new TEntityField("EventId",     typeof(string),   false, false),
      new TEntityField("EventType",   typeof(string),   false, false),
      new TEntityField("SourceNode",  typeof(string),   false, false),
      new TEntityField("Time",        typeof(DateTime), false, false),
      new TEntityField("RecieveTime", typeof(DateTime), false, false),
      new TEntityField("Message",     typeof(string),   false, false),
      new TEntityField("Severity",    typeof(short),    false, false)
    };

    TGenericEntity EntityToolUsedEvent = new TGenericEntity(lstFieldsToolUsedEvent, Id);

    // ToolEvent values
    EntityToolUsedEvent.setValue("FacilityComponentId", strFacilityComponentId);
    EntityToolUsedEvent.setValue("Identifier", EntityIdentifier);
    EntityToolUsedEvent.setValue("UsageOffset", iUsageOffset);
    EntityToolUsedEvent.setValue("ToolLifeConsumtion", iToolLifeConsumption);

    // BaseEventType values
    DateTime dtNow = TDateTime.UtcNowHighRes;
    EntityToolUsedEvent.setValue("EventId", "EventId123");
    EntityToolUsedEvent.setValue("EventType", "ToolUsedEventType");
    EntityToolUsedEvent.setValue("SourceNode", "ProductionManagement");
    EntityToolUsedEvent.setValue("Time", dtNow);
    EntityToolUsedEvent.setValue("RecieveTime", dtNow);
    EntityToolUsedEvent.setValue("Message", "The ToolUsedEventType has been fired.");
    EntityToolUsedEvent.setValue("Severity", (short)500);

    return EntityToolUsedEvent;
  }
}
