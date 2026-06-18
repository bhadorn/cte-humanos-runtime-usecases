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

using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Entity;
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.UHAL.Script;
using System;
using System.Collections.Generic;
using HumanOS.Kernel.Processing;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts;

/// <summary>
/// Handles the request transport command
/// </summary>
public class TRequestTransportCommand : TAbstractLogicScriptObject
{
  ///<see cref="TAbstractLogicScriptObject"/>
  public override async Task executeCommandAsync(IKernelAccess Kernel, IGroupRelation DeviceNode, ICommandCallContext CallContext, CancellationToken Token)
  {
    int iTransportClass = CallContext.getInputArgumentValue<int>("transportClass");
    int iTransportObjectClass = CallContext.getInputArgumentValue<int>("transportObjectClass");
    TGenericEntity[] aSourceLocation = CallContext.getInputArgumentValue<TGenericEntity[]>("sourceLocation");
    TGenericEntity SourceObject = CallContext.getInputArgumentValue<TGenericEntity>("sourceObject");
    TGenericEntity[] aDestinationLocation = CallContext.getInputArgumentValue<TGenericEntity[]>("destinationLocation");
    TGenericEntity DestinationObject = CallContext.getInputArgumentValue<TGenericEntity>("destinationObject");
    TGenericEntity[] aApplicationData = CallContext.getInputArgumentValue<TGenericEntity[]>("applicationData");
    int iErrorCode = CallContext.getOutputArgumentValue<int>("errorCode");

    Logger.writeInfo($"RequestTransportCommand: '{DeviceNode.Name}'.");
    Logger.writeInfo($"  iTransportClass:       {iTransportClass}");
    Logger.writeInfo($"  iTransportObjectClass: {iTransportObjectClass}");
    Logger.writeInfo($"  aSourceLocation:       {printEntities(aSourceLocation)}");
    Logger.writeInfo($"  SourceObject:          {printEntity(SourceObject)}");
    Logger.writeInfo($"  aDestinationLocation:  {printEntities(aDestinationLocation)}");
    Logger.writeInfo($"  DestinationObject:     {printEntity(DestinationObject)}");
    Logger.writeInfo($"  aApplicationData:      {printEntities(aApplicationData)}");

    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>
  /// Print multiple entities
  /// </summary>
  private string printEntities(TGenericEntity[] naEntities)
  {
    string strReturn = "";
    foreach (var nEntity in naEntities)
    {
      strReturn += printEntity(nEntity);
    }
    return strReturn;
  }

  /// <summary>
  /// Print single entity with its properties
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
