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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Loads a job to the storage
  /// </summary>
  public class TUnloadJob : TAbstractLogicScriptObject
  {
    ///<see cref="TAbstractLogicScriptObject"/>
    public override void executeCommand(IKernelAccess Kernel,
                                        IGroupRelation DeviceNode,
                                        Dictionary<string, string> dicProperties,
                                        Dictionary<string, object> dicInputArguments,
                                        Dictionary<string, object> dicOutputArguments)
    {
      string strJobId    = TCommandHelper.getArgument<string>(dicInputArguments, "JobId");

      Logger.writeInfo($"Unkload Job '{DeviceNode.Name}'.");
      Logger.writeInfo($"  JobId: {strJobId}");
      
      IDataNode<int>    StoredParts = DeviceNode.queryNode(n => n.Name == "StoredParts") as IDataNode<int>; 
      IDataNode<string> JobRegistry = DeviceNode.queryNode(n => n.Name == "JobRegistry") as IDataNode<string>;
      JObject jData;
      try
      {
        jData = JObject.Parse(JobRegistry.Value);
      }
      catch
      {
        jData = new JObject();
      }
      
      int iNumberOfParts = 0;
      if (jData.TryGetValue(strJobId, out JToken jJobToken) && jJobToken is JObject jJob)
      {
        iNumberOfParts = (int)jJob["NumberOfParts"];
        jData.Remove(strJobId);
        
        TCommandHelper.setArgument(dicOutputArguments, "NumberOfParts", iNumberOfParts);
      }
      else
      {
        throw new ArgumentException($"Could not find job '{strJobId}'.");
      }
      
      //Update Job registry
      JobRegistry.passValue(jData.ToString());
      Logger.writeVerbose($"JobRegistry: {jData}");

      //Decrease the number of stored parts
      StoredParts.passValue(StoredParts.Value - iNumberOfParts);
    }
  }
}
