/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2024 – www.cybertech.swiss         *
 *****************************************************************************
 * Project: HumanOS (R)
 * Date   : 2025
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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Teaches waypoints to the robot
  /// </summary>
  public class TBlankUhalLogicScriptObject : TAbstractLogicScriptObject
  {
    ///<see cref="TAbstractLogicScriptObject"/>
    public override void executeCommand(IKernelAccess Kernel,
                                        IGroupRelation DeviceNode,
                                        Dictionary<string, string> dicProperties,
                                        Dictionary<string, object> dicInputArguments,
                                        Dictionary<string, object> dicOutputArguments)
    {
      //Check if the start device is available
      string strStart = TCommandHelper.getArgument<string>(dicInputArguments, "Start");
      if(Kernel.NodeSpace.queryNode(n => n.Name == strStart && n.hasProperty("DriverId")) == null)
      {
        throw new ArgumentException($"Device '{strStart}' not found.");
      }
      
      //Check if the end device is available
      string strEnd   = TCommandHelper.getArgument<string>(dicInputArguments, "End");
      if(Kernel.NodeSpace.queryNode(n => n.Name == strEnd && n.hasProperty("DriverId")) == null)
      {
        throw new ArgumentException($"Device '{strEnd}' not found.");
      }
      
      JArray ajWayPonts = JArray.Parse(TCommandHelper.getArgument<string>(dicInputArguments, "WayPoints"));
      
      Logger.writeInfo($"Teach Robot '{DeviceNode.Name}'.");
      Logger.writeInfo($"  Start:     {strStart}");
      Logger.writeInfo($"  End:       {strEnd}");
      Logger.writeInfo($"  WayPoints: {ajWayPonts.ToString()}");
      
      IDataNode<string> Node = DeviceNode.queryNode(n => n.Name == "TaughtWayPoints") as IDataNode<string>;
      JObject jData;
      try
      {
        jData = JObject.Parse(Node.Value);
      }
      catch
      {
        jData = new JObject();
      }
      
      JObject jDataSet = new JObject();
      jDataSet["Start"] = strStart;
      jDataSet["End"] = strEnd;
      jDataSet["WayPoints"] = ajWayPonts;
      
      jData[$"{strStart}_{strEnd}"] = jDataSet;
      
      Node.passValue(jData.ToString());
      Logger.writeVerbose($"Teach DataSet: {jData}");
    }
  }
}
