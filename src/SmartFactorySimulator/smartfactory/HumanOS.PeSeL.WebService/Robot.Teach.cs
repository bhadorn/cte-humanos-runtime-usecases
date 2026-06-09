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
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.PeSeL.Script;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Utils;
using HumanOS.PeSeL.WebService.Script;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of HumanOS REST API script
  /// </summary>
  public class TRobotTeachScript : TAbstractWebScriptObject
  {
    ///<see cref="TAbstractWebScriptObject"/>
    public override string handleDelete(IKernelAccess Kernel,
                                        HttpContext HttpContext,
                                        TPayloadProcessingContext Context,
                                        List<string> lstPath,
                                        Dictionary<string, string> dicParams,
                                        ref EContentType reContentType,
                                        string strData)
    {
      throw new NotImplementedException();
    }

    ///<see cref="TAbstractWebScriptObject"/>
    public override string handleGet(IKernelAccess Kernel,
                                     HttpContext HttpContext,
                                     TPayloadProcessingContext Context,
                                     List<string> lstPath,
                                     Dictionary<string, string> dicParams,
                                     ref EContentType reContentType)
    {
      throw new NotImplementedException();
    }

    ///<see cref="TAbstractWebScriptObject"/>
    public override string handlePatch(IKernelAccess Kernel,
                                       HttpContext HttpContext,
                                       TPayloadProcessingContext Context,
                                       List<string> lstPath,
                                       Dictionary<string, string> dicParams,
                                       ref EContentType reContentType,
                                       string strData)
    {
      throw new NotImplementedException();
    }

    ///<see cref="TAbstractWebScriptObject"/>
    public override string handlePost(IKernelAccess Kernel,
                                      HttpContext HttpContext,
                                      TPayloadProcessingContext Context,
                                      List<string> lstPath,
                                      Dictionary<string, string> dicParams,
                                      ref EContentType reContentType,
                                      string strData,
                                      HttpResponse Response)
    {
      JObject jData = JObject.Parse(strData);
      
      string strRobotId = jData["RobotId"].ToString();
      string strCommandName = "Teach";
      
      IGroupRelation nDeviceNode = Kernel.NodeSpace.queryNodeLocally(n => n.Name == strRobotId) as IGroupRelation;
      if (nDeviceNode != null)
      {
        ICommandNode nCommand = nDeviceNode.queryNodeLocally(n => n.Name == strCommandName) as ICommandNode;
        if (nCommand != null)
        {
          TCommandArgs Args = new TCommandArgs();
          Args.Input["Start"] = jData["Start"].ToString();
          Args.Input["End"] = jData["End"].ToString();
          Args.Input["WayPoints"] = jData["WayPoints"].ToString();
          
          TCommandResult Result = TCommandHelper.call(nCommand, Args);
          if (Result.State != EProcessingState.Good)
          {
            throw new ArgumentException($"Command '{strCommandName}' failed. {Result.ErrorMessage}");
          }
        }
        else
        {
          throw new ArgumentException($"Command '{strCommandName}' not found.");
        }
      }
      else
      {
        throw new ArgumentException($"Roboter '{strRobotId}' not found.");
      }
      return "OK";
    }
  }
}
