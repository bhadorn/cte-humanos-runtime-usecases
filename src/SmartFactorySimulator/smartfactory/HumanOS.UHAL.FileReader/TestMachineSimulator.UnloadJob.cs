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
  /// Unloads the job from a test machine
  /// </summary>
  public class TUnloadJobToTestMachine : TAbstractLogicScriptObject
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
      IDataNode<string> TrayOut = DeviceNode.queryNode(n => n.Name == "TrayOut") as IDataNode<string>;

      //Wait until the tray has parts
      while(TrayOut.Value.isEmpty() && !Token.IsCancellationRequested)
      {
        await Task.Delay(WaitingTime, Token);
      }
      Token.ThrowIfCancellationRequested();

      try
      {
        JObject jData = JObject.Parse(TrayOut.Value);
        string strJobId = (string)jData["JobId"];
        int iNumberOfParts = (int)jData["NumberOfParts"];

        Logger.writeInfo($"Unload Job '{DeviceNode.Name}'.");
        Logger.writeInfo($"  JobId:         {strJobId}");
        Logger.writeInfo($"  NumberOfParts: {iNumberOfParts}");

        TCommandHelper.setArgument(dicOutputArguments, "JobId", strJobId);
        TCommandHelper.setArgument(dicOutputArguments, "NumberOfParts", iNumberOfParts);
      }
      finally
      {
        TrayOut.passValue("");
      }
    }
  }
}
