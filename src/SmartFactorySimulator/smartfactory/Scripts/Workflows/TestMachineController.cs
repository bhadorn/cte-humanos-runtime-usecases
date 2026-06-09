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

using CyberTech;
using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Workflow.Activity;
using HumanOS.Kernel.Workflow.Instruction;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of a workflow operation script
  /// </summary>
  public class TTestMachineController : TAbstractOperationScriptObject
  {
    private const int WaitingTime = 100;
    
    ///<see cref="TAbstractOperationScriptObject"/>
    public override async Task runAsync(IKernelAccess Kernel, IActivity Activity, CancellationToken CancellationToken)
    {
      IGroupRelation Device     = Activity.Context.getValue<IGroupRelation>("Device");
      IGroupRelation Controller = Activity.Context.getValue<IGroupRelation>("Controller");

      TrayIn           = Device.queryNodeLocally(n => n.Name == "TrayIn") as IDataNode<string>;
      TrayOut          = Device.queryNodeLocally(n => n.Name == "TrayOut") as IDataNode<string>;

      AlarmState       = Controller.queryNodeLocally(n => n.Name == "AlarmState") as IDataNode<int>;
      RunningState     = Controller.queryNodeLocally(n => n.Name == "RunningState") as IDataNode<int>;
      CurrentJobId     = Controller.queryNodeLocally(n => n.Name == "CurrentJobId") as IDataNode<string>;
      PartsToProcess   = Controller.queryNodeLocally(n => n.Name == "PartsToProcess") as IDataNode<int>;
      PartsProcessed   = Controller.queryNodeLocally(n => n.Name == "PartsProcessed") as IDataNode<int>;
      TotalPartCounter = Controller.queryNodeLocally(n => n.Name == "TotalPartCounter") as IDataNode<int>;

      double fCycleTime = Device.getProperty<double>("CycleTime");

      while(!CancellationToken.IsCancellationRequested)
      {
        //Job is done 
        if (CurrentJobId.Value.isEmpty())
        {
          string strNewJob = TrayIn.Value;
        
          JObject jNewJob = null;
          if (strNewJob.isNotEmpty())
          {
            try
            {
              jNewJob = JObject.Parse(strNewJob);
            }
            catch { }
          }
        
          //Load new job into the machine
          if (jNewJob != null)
          {
            await Task.Delay(200); //Time to take the tray into the machine
            try
            {
              PartsProcessed.passValue(0);
              PartsToProcess.passValue((int)jNewJob["NumberOfParts"]);
              CurrentJobId.passValue((string)jNewJob["JobId"]);
              TrayIn.passValue(""); //Tray not in input slot anymore
            }
            catch 
            {
              PartsToProcess.passValue(0);
              CurrentJobId.passValue("");
              AlarmState.passValue(1);
            }
          }
          //Wait for new jobs
          else
          {
            await Task.Delay(WaitingTime);
          }
        }
        
        //Process part
        if (PartsToProcess.Value > 0)
        {
          AlarmState.passValue(0);
          RunningState.passValue(1);
          
          //Waiting to process part
          await Task.Delay(TFloat.roundInt(fCycleTime*1000));
          
          //Process part
          PartsToProcess.passValue(PartsToProcess.Value - 1);
          PartsProcessed.passValue(PartsProcessed.Value + 1);
          TotalPartCounter.passValue(TotalPartCounter.Value + 1);
        }
        
        //Job done and output is empty
        if (PartsToProcess.Value == 0 && CurrentJobId.Value.isNotEmpty())
        {
          if (TrayOut.Value.isEmpty())
          {
            JObject jOut = new JObject();
            jOut["NumberOfParts"] = PartsProcessed.Value;
            jOut["JobId"] = CurrentJobId.Value;
            
            CurrentJobId.passValue("");
            PartsProcessed.passValue(0);
            TrayOut.passValue(jOut.ToString());
          }
          else
          {
            await Task.Delay(WaitingTime);
          }
          RunningState.passValue(0);
        }
      }
    }
    
    private IDataNode<int> AlarmState          { get; set; }
    private IDataNode<int> RunningState        { get; set; }
    private IDataNode<int> PartsToProcess      { get; set; }
    private IDataNode<int> PartsProcessed      { get; set; }
    private IDataNode<int> TotalPartCounter    { get; set; }
    private IDataNode<string> CurrentJobId     { get; set; }

    private IDataNode<string> TrayIn           { get; set; }
    private IDataNode<string> TrayOut          { get; set; }
  }
}
