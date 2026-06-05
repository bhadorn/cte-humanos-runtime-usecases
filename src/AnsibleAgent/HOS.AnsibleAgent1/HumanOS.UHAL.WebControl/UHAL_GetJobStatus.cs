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
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example for UHAL logic
  /// </summary>
  public class TUhalGetJobStatus : TAbstractLogicScriptObject
  {
    #region Constants
    
    private const string ARG_ProjectId = "ProjectId";
    private const string ARG_TaskId = "TaskId";
    private const string ARG_AuthCookie = "AuthCookie";
    private const string ARG_Status = "Status";
    
    #endregion
  
    ///<see cref="TAbstractLogicScriptObject"/>
    public override void executeCommand(IKernelAccess Kernel, IGroupRelation DeviceNode, Dictionary<string, string> dicProperties, Dictionary<string, object> dicInputArguments, Dictionary<string, object> dicOutputArguments)
    {
      string strAuthenticationKey = "";
      try
      {
        int iProjectId = TCommandHelper.getArgument<int>(dicInputArguments, "ProjectId");
        int iTaskId = TCommandHelper.getArgument<int>(dicInputArguments, "TaskId");

        //1. login
        strAuthenticationKey = login(DeviceNode);
        
        TCommandArgs Args = new TCommandArgs();
        Args.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args.Input[ARG_TaskId] = iTaskId;
        Args.Input[ARG_ProjectId] = iProjectId;
        TCommandResult Result = TCommandHelper.call(DeviceNode, "GetTaskStatus", Args);
        if (Result.State != EProcessingState.Good)
        { 
          throw new ArgumentException($"Failed to get task status. {Result.ErrorInfo}");
        }
        
        string strStatus = TCommandHelper.getArgumentOrDefault<string>(Args.Output, ARG_Status, "");
        Logger.writeDebug($"Status returned: {strStatus}");
        switch(strStatus)
        {
          case "running":
          case "waiting": //fall through
            dicOutputArguments[ARG_Status] = 0;
          break;
          
          case "success":
            dicOutputArguments[ARG_Status] = 1;
          break;
          
          case "failed":
          default:
            dicOutputArguments[ARG_Status] = 2;
          break;
        }
      }
      finally
      {
        tryLogout(DeviceNode, strAuthenticationKey);
      }
    }
    
    ///<summary>
    /// Login to semaphore
    ///</summary>
    private string login(IGroupRelation DeviceNode)
    {
      TCommandArgs Args = new TCommandArgs();
      TCommandResult Result = TCommandHelper.call(DeviceNode, "LoginUser", Args);
      if (Result.State != EProcessingState.Good)
      {
        throw new ArgumentException($"Failed to login. {Result.ErrorInfo}");
      }
      Logger.writeDebug("Successfully login.");
      return Args.getOutputArgument<string>(ARG_AuthCookie);
    }
    
    ///<summary>
    /// Logout from semaphore
    ///</summary>
    private void tryLogout(IGroupRelation DeviceNode, string strAuthenticationKey)
    {
      try
      {
        TCommandArgs Args = new TCommandArgs();
        Args.Input[ARG_AuthCookie] = strAuthenticationKey;
        TCommandResult Result = TCommandHelper.call(DeviceNode, "LogoutUser", Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to logout. {Result.ErrorInfo}");
        }
        Logger.writeDebug("Successfully logout.");
      }
      catch(Exception Exc) when (!Exc.isCancelException())
      {
        Logger.writeWarning(Exc.Message);
      }
    }    
  }
}
