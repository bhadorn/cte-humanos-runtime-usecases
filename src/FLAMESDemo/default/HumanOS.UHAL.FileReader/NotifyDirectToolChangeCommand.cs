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
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.UHAL.Script;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using HumanOS.Kernel.DataModel.Entity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts;

/// <summary>
/// Handling of NotifyDirectToolChange command
/// </summary>
public class TNotifyDirectToolChangeCommand : TAbstractLogicScriptObject
{
  ///<see cref="TAbstractLogicScriptObject"/>
  //public override void executeCommand(IKernelAccess Kernel,
  //                                    IGroupRelation DeviceNode,
  //                                    Dictionary<string, string> dicProperties,
  //                                    Dictionary<string, object> dicInputArguments,
  //                                    Dictionary<string, object> dicOutputArguments)
  public override async Task executeCommandAsync(IKernelAccess Kernel, IGroupRelation DeviceNode, ICommandCallContext CallContext, CancellationToken Token)
  {
    // Read Arguments
    TGenericEntity LoadedTool = CallContext.getInputArgumentValue<TGenericEntity>("loadedTool");
    TGenericEntity UnloadedTool = CallContext.getInputArgumentValue<TGenericEntity>("unloadedTool");
    TGenericEntity[] aApplicationData = CallContext.getInputArgumentValue<TGenericEntity[]>("applicationData");

    Logger.writeInfo($"NotifyDirectToolChangeCommand: '{DeviceNode.Name}'.");
    Logger.writeInfo($"  LoadedTool:       {printEntity(LoadedTool)}");
    Logger.writeInfo($"  UnloadedTool:     {printEntity(UnloadedTool)}");
    Logger.writeInfo($"  aApplicationData: {printEntities(aApplicationData)}");

    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>
  /// Call a command inside Controller group node
  /// </summary>
  private void callCommand(IKernelAccess Kernel, string strCommandName, TCommandArgs Args)
  {
    // Find Controller group node
    IGroupRelation? nDeviceNode = Kernel.NodeSpace.queryNodeLocally(n => n.Name == "Controller") as IGroupRelation;
    if (nDeviceNode != null)
    {
      IGroupRelation? nCommands = Kernel.NodeSpace.queryNodeLocally(n => n.Name == "Commands") as IGroupRelation;
      if (nCommands != null)
      {
        // Call command
        TCommandResult Result = TCommandHelper.call(nCommands, strCommandName, Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Command '{strCommandName}' failed. {Result.ErrorMessage}");
        }
      }
      else
      {
        throw new ArgumentException($"Group 'Commands' not found.");
      }
    }
    else
    {
      throw new ArgumentException($"Group 'Controller' not found.");
    }

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
