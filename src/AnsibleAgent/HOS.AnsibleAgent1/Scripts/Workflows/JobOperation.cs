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
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.Workflow.Activity;
using HumanOS.Kernel.Workflow.Instruction;
using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of a workflow operation script
  /// </summary>
  public class Blank_PeMiLOperationScriptObject : TAbstractOperationScriptObject
  {
  
    #region Constants
    
    private const string ARG_EntityId = "EntityId";
    private const string ARG_Arguments = "Arguments";
    private const string ARG_Environment = "Environment";
    private const string ARG_Inventory = "Inventory";
    private const string ARG_JobName = "JobName";
    private const string ARG_ProjectId = "ProjectId";
    private const string ARG_TaskId = "TaskId";
    private const string ARG_Status = "Status";
    private const string ARG_Content = "Content";
    private const string ARG_Feedback = "Feedback";
    
    #endregion
  
    ///<see cref="TAbstractOperationScriptObject"/>
    public override async Task runAsync(IKernelAccess Kernel, IActivity Activity, CancellationToken CancellationToken)
    {
      StringBuilder LogEntries = new StringBuilder();
      bool bFailed = false;
      bool bContinueOnError = false;

      writeInfo(LogEntries, $"OPERATION for Job '{Activity.Name}'");

      try
      {
        string strInventory = Activity.getProperty<string>("Inventory", "");
        string strTriggerAction = Activity.getProperty<string>("TriggeringAction", "");
        bContinueOnError = Activity.getProperty<bool>("ContinueOnError", false);
      
        writeInfo(LogEntries, $"  Trigger      : {strTriggerAction}");
        writeInfo(LogEntries, $"  CollectionId : {Activity.getProperty<Guid>("EntityCollectionId")}");
        writeInfo(LogEntries, $"  EntityId     : {Activity.getProperty<Guid>("EntityId")}");
        writeInfo(LogEntries, $"  Arguments    : {Activity.getProperty<string>("Arguments", "")}");
        writeInfo(LogEntries, $"  Environment  : {Activity.getProperty<string>("Environment", "")}");
        writeInfo(LogEntries, $"  Inventory    : {strInventory}");
        writeInfo(LogEntries, $"  IgnoreError  : {bContinueOnError}");

        //Gets the semaphore API device
        IGroupRelation nSemaphoreDevice = Kernel.NodeSpace.queryNodeLocally(n => n.hasProperty<bool>("EnableSemaphore", true)) as IGroupRelation;
        if (nSemaphoreDevice == null)
        {
          throw new ArgumentException("No device for sempahore API found.");
        }
      
        //Starts the job
        TCommandArgs Args = new TCommandArgs();
        Args.Input[ARG_EntityId] = Activity.getProperty<Guid>("EntityId");
        Args.Input[ARG_Arguments] = Activity.getProperty<string>("Arguments");
        Args.Input[ARG_Environment] = Activity.getProperty<string>("Environment");
        Args.Input[ARG_Inventory] = strInventory;
        Args.Input[ARG_JobName] = Activity.Name;
      
        TCommandResult Result = TCommandHelper.call(nSemaphoreDevice, "StartJob", Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to start job for semaphore. {Result.ErrorInfo}");
        }
      
        int iProjectId = TCommandHelper.getArgumentOrDefault<int>(Args.Output, ARG_ProjectId, 0);
        int iTaskId = TCommandHelper.getArgumentOrDefault<int>(Args.Output, ARG_TaskId, 0);
        if (iProjectId == 0)
        {
          throw new ArgumentException($"Failed to start job for semaphore. Invalid project id returned.");
        }
        if (iTaskId == 0)
        {
          throw new ArgumentException($"Failed to start job for semaphore. Invalid task id returned.");
        }
      
        //Wait for job termination
        bool bTaskRunning = true;
        while(bTaskRunning && !CancellationToken.IsCancellationRequested)
        {
          await Task.Delay(10000, CancellationToken);

          Args = new TCommandArgs();
          Args.Input[ARG_ProjectId] = iProjectId;
          Args.Input[ARG_TaskId] = iTaskId;
          Result = TCommandHelper.call(nSemaphoreDevice, "GetJobStatus", Args);
        
          if (Result.State != EProcessingState.Good)
          {
            bTaskRunning = false;
            writeError(LogEntries, Result.ErrorMessage);
          }
          else
          {
            switch(TCommandHelper.getArgumentOrDefault<int>(Args.Output, ARG_Status, 0))
            {
              case 0: //Running -> continue
              break;
              
              case 1: //success
                bTaskRunning = false;
              break;
              
              case 2: //failed
              default:
                bFailed = true;
                bTaskRunning = false;
                writeError(LogEntries, "Job failed.");
              break;
            }
          }
        } //end while

        //This must be at the end of the loop. 
        // We have to wait before we can get the JobOutput
        await Task.Delay(10000, CancellationToken);

        Args = new TCommandArgs();
        Args.Input[ARG_TaskId] = iTaskId;
        Args.Input[ARG_ProjectId] = iProjectId;
        Result = TCommandHelper.call(nSemaphoreDevice, "GetJobOutput", Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to get job output. {Result.ErrorMessage}");
        }
        Activity.Context.setValue($"{Activity.Name}:Feedback", TCommandHelper.getArgument<string>(Args.Output, ARG_Feedback));
        
        try
        {
          JArray jLogs = JArray.Parse(TCommandHelper.getArgument<string>(Args.Output, ARG_Content));
          foreach(JObject jEntry in jLogs)
          {
            LogEntries.AppendLine($"{(string)jEntry["time"]}: {(string)jEntry["output"]}");
          }
        }
        catch
        {
          LogEntries.AppendLine(TCommandHelper.getArgument<string>(Args.Output, ARG_Content));
        }
      }
      catch(Exception Exc) when (!Exc.isCancelException())
      {
        writeError(LogEntries, Exc.Message);
        bFailed = true;
      }
      Activity.Context.setValue($"{Activity.Name}:Log", LogEntries.ToString());

      //Abort the activity workflow
      if (bFailed)
      {
        Activity.Context.setValue($"{Activity.Name}:Error", "Job Failed.");
        if (!bContinueOnError)
        {
          throw new ArgumentException("Job failed.");
        }
      }
    }
    
    ///<summary>
    /// write info log entry
    ///</summary>
    private void writeInfo(StringBuilder LogEntries, string strLog)
    {
      Logger.writeInfo(strLog);
      LogEntries.AppendLine($"INF: {strLog}");
    }
    
    ///<summary>
    /// write error log entry
    ///</summary>
    private void writeError(StringBuilder LogEntries, string strLog)
    {
      Logger.writeError(strLog);
      LogEntries.AppendLine($"ERR: {strLog}");
    }
  }
}
