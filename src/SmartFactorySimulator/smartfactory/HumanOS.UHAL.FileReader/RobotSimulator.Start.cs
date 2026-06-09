/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2024 – www.cybertech.swiss         *
 *****************************************************************************
 * Project: HumanOS (R)
 * Date   : 2024
 *****************************************************************************
 * License:                                                                  *
 *   This library is protected software; you are not allowed to redistribute *
 *   whole or part of it to other companies or external persons without the  *
 *   authorization of the CEO CyberTech Engineering GmbH.                    *
 *****************************************************************************/

using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.UHAL.Script;
using System;
using System.Collections.Generic;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Starts the robot
  /// </summary>
  public class TStartRobotScript : TAbstractLogicScriptObject
  {
    ///<see cref="TAbstractLogicScriptObject"/>
    public override void executeCommand(IKernelAccess Kernel,
                                        IGroupRelation DeviceNode,
                                        Dictionary<string, string> dicProperties,
                                        Dictionary<string, object> dicInputArguments,
                                        Dictionary<string, object> dicOutputArguments)
    {
      Logger.writeInfo($"Start Robot '{DeviceNode.Name}'.");
      Logger.writeInfo($"  JobId: {TCommandHelper.getArgument<string>(dicInputArguments, "JobId")}");
      Logger.writeInfo($"  Speed: {TCommandHelper.getArgument<double>(dicInputArguments, "Speed")}");
      
      IGroupRelation nTray = DeviceNode.queryNode(n => n.Name == "Tray") as IGroupRelation;
      if (nTray == null)
      {
        throw new ArgumentException($"Could not find tray of '{DeviceNode.Name}'.");
      }
      
      IGroupRelation nController = DeviceNode.queryNode(n => n.Name == "Controller") as IGroupRelation;
      if (nController == null)
      {
        throw new ArgumentException($"Could not find Controller of '{DeviceNode.Name}'.");
      }
      IDataNode<string> CurrentJobId = nTray.queryNode(n => n.Name == "CurrentJobId") as IDataNode<string>;
      IDataNode<double> SetSpeed     = nController.queryNode(n => n.Name == "SetSpeed") as IDataNode<double>;
      IDataNode<int> RunningState    = nController.queryNode(n => n.Name == "RunningState") as IDataNode<int>;
      
      CurrentJobId.passValue("");
      SetSpeed.passValue(TCommandHelper.getArgument<double>(dicInputArguments, "Speed"));
      CurrentJobId.passValue(TCommandHelper.getArgument<string>(dicInputArguments, "JobId"));
      
      RunningState.passValue(1);
    }
  }
}
