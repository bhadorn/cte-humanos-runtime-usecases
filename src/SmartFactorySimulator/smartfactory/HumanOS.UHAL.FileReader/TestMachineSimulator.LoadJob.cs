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
using System.Threading;
using System.Threading.Tasks;


namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example for UHAL logic
  /// </summary>
  public class TLoadJobToTestMachine : TAbstractLogicScriptObject
  {
    private const int WaitingTime = 100;

    ///<see cref="TAbstractLogicScriptObject"/>
    public override async Task executeCommandAsync(IKernelAccess Kernel,
                                        IGroupRelation DeviceNode,
                                        Dictionary<string, string> dicProperties,
                                        Dictionary<string, object> dicInputArguments,
                                        Dictionary<string, object> dicOutputArguments,
                                        CancellationToken Token)
    {
      string strJobId    = TCommandHelper.getArgument<string>(dicInputArguments, "JobId");
      int iNumberOfParts = TCommandHelper.getArgument<int>(dicInputArguments, "NumberOfParts");
      
      Logger.writeInfo($"Load Job '{DeviceNode.Name}'.");
      Logger.writeInfo($"  JobId:         {strJobId}");
      Logger.writeInfo($"  NumberOfParts: {iNumberOfParts}");
      
      IDataNode<string> TrayIn = DeviceNode.queryNode(n => n.Name == "TrayIn") as IDataNode<string>;
      
      //Wait until the tray becoms available
      while(TrayIn.Value.isNotEmpty() && !Token.IsCancellationRequested)
      {
        await Task.Delay(WaitingTime, Token);
      }
      
      Token.ThrowIfCancellationRequested();
      
      if (TrayIn.Value.isEmpty())
      {
        JObject jData = new JObject();
        jData["JobId"] = strJobId;
        jData["NumberOfParts"] = iNumberOfParts;
        TrayIn.passValue(jData.ToString());
      }
    }
  }
}
