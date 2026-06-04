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

using CyberTech.Threading;
using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    private const string ARG_Content = "Content";
    private const string ARG_Feedback = "Feedback";
    
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
        
        int iMaxTry = 5;
        do
        {
          TCommandArgs Args = new TCommandArgs();
          Args.Input[ARG_AuthCookie] = strAuthenticationKey;
          Args.Input[ARG_TaskId] = iTaskId;
          Args.Input[ARG_ProjectId] = iProjectId;
          TCommandResult Result = TCommandHelper.call(DeviceNode, "GetTaskOutput", Args);
          if (Result.State != EProcessingState.Good)
          { 
            throw new ArgumentException($"Failed to get task output. {Result.ErrorInfo}");
          }
        
          //Try to extract the Json
          string strContent = TCommandHelper.getArgumentOrDefault<string>(Args.Output, ARG_Content, "");
          if (extractFeedback(strContent, out string strFeedback))
          {
            iMaxTry = 0;
          }
          else
          {
            TThread.sleep(5);
            iMaxTry--;
          }
          dicOutputArguments[ARG_Content] = strContent;
          dicOutputArguments[ARG_Feedback] = strFeedback;
        } while (iMaxTry > 0);
      }
      finally
      {
        tryLogout(DeviceNode, strAuthenticationKey);
      }
    }
    
    ///<summary>
    /// Extracts the feedback JSON from the output content
    ///</summary>
    private bool extractFeedback(string strRawContent, out string ostrFeedback)
    {
      bool bRetval = false;
      StringBuilder Data = new StringBuilder();
      JObject jRetval;
      try
      {
        JArray jData = JArray.Parse(strRawContent);
        foreach(JObject jObj in jData)
        {
          Data.AppendLine((string)jObj["output"]);
        }
        
        Capture nCap = Regex.Match(Data.ToString(), @"\{[\s\S]*?\r?\n\}").Captures.LastOrDefault();
        if (nCap != null)
        {
          jRetval = JObject.Parse(nCap.Value);
        }
        else
        {
          throw new ArgumentException("Failed to extract the JSON from output.");
        }
        bRetval = false;
      }
      catch (Exception Exc)
      {
        jRetval = new JObject();
        jRetval["Error"] = Exc.Message;
        jRetval["Data"] = Data.ToString();
      }
      ostrFeedback = jRetval.ToString();
      return bRetval;
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