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
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.Workflow.Activity;
using HumanOS.Kernel.Workflow.Instruction;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of a workflow operation script
  /// </summary>
  public class TRobotController : TAbstractOperationScriptObject
  {
  
    #region Constants
    
    private const double TotalBatteryCapacity = 20;
    private const int WaitingTime = 100;
    
    #endregion
    
    ///<see cref="TAbstractOperationScriptObject"/>
    public override async Task runAsync(IKernelAccess Kernel, IActivity Activity, CancellationToken CancellationToken)
    {
      CurrentPos = new TMovementJM();

      IGroupRelation Controller = Activity.Context.getValue<IGroupRelation>("Controller");
      IGroupRelation Battery    = Activity.Context.getValue<IGroupRelation>("Battery");
      IGroupRelation VHub       = Activity.Context.getValue<IGroupRelation>("vHub");
      IGroupRelation Tray       = Activity.Context.getValue<IGroupRelation>("Tray");

      AlarmState        = Controller.queryNodeLocally(n => n.Name == "AlarmState") as IDataNode<int>;
      RunningState      = Controller.queryNodeLocally(n => n.Name == "RunningState") as IDataNode<int>;
      PositionX         = Controller.queryNodeLocally(n => n.Name == "PositionX") as IDataNode<double>;
      PositionY         = Controller.queryNodeLocally(n => n.Name == "PositionY") as IDataNode<double>;
      SetSpeed          = Controller.queryNodeLocally(n => n.Name == "SetSpeed") as IDataNode<double>;
      RunningMeter      = Controller.queryNodeLocally(n => n.Name == "RunningMeters") as IDataNode<double>;
      SetTravelDistance = Controller.queryNodeLocally(n => n.Name == "SetTravelDistance") as IDataNode<double>;
      CurrentProgram    = Controller.queryNodeLocally(n => n.Name == "CurrentProgram") as IDataNode<string>;
      BatteryCapacity   = Battery.queryNodeLocally(n => n.Name == "BatteryCapacity") as IDataNode<double>;
      BatteryLevel      = Battery.queryNodeLocally(n => n.Name == "BatteryLevel") as IDataNode<double>;
      PowerConsumption  = Battery.queryNodeLocally(n => n.Name == "PowerConsumption") as IDataNode<double>;
      SetTravelTime     = Controller.queryNodeLocally(n => n.Name == "SetTravelTime") as IDataNode<double>;
      CurrentTravelTime = Controller.queryNodeLocally(n => n.Name == "CurrentTravelTime") as IDataNode<double>;
      CurrentJobId      = Tray.queryNodeLocally(n => n.Name == "CurrentJobId") as IDataNode<string>;
      
      ReadWayPointsCommand = VHub.queryNodeLocally(n => n.Name == "ReadWayPoints") as ICommandNode;
      CurrentJobId.addEventOnValueChangedAndFireEvent(onCurrentJobChanged);

      Stopwatch Watch = new Stopwatch();
      Watch.Start();
      while(!CancellationToken.IsCancellationRequested)
      {
        if (RunningState.Value == 1)
        {
          try
          {
            AlarmState.passValue(0);
            await runAsync(JsonConvert.DeserializeObject<List<TMovementJM>>(CurrentProgram.Value), CancellationToken);
          }
          catch(Exception Exc) when (Exc.isNotCancelException())
          {
            AlarmState.passValue(1);
            RunningState.passValue(0);
            Logger.writeError(Exc.Message);
          }
          Watch.Reset();
          Watch.Restart();
        }
        else
        {
          DateTime LastTime = DateTime.UtcNow;
          await Task.Delay(WaitingTime);
          DateTime Now = DateTime.UtcNow;
          
          //Power consumption in standby
          consumePower(0.01, (Now - LastTime).TotalHours);
          
          if (Watch.ElapsedMilliseconds > 60000)
          {
            await readWayPointsAsync(CurrentJobId.Value);
            Watch.Reset();
            Watch.Restart();
          }
        }
      }
    }
    
    [DataContract]
    private class TMovementJM
    {
      public TMovementJM(){}
      public TMovementJM(double fPosX, double fPosY) { PosX = fPosX; PosY = fPosY; }
      
      [DataMember]
      public double PosX { get; set; }

      [DataMember]
      public double PosY { get; set; }
      
      public double Length => Math.Sqrt(PosX*PosX + PosY*PosY);
      
      public TMovementJM calNewPos(TMovementJM StartPos, double fRunningIndex)
      {
        return new TMovementJM(StartPos.PosX * (1 -fRunningIndex) + fRunningIndex*PosX, StartPos.PosY * (1 -fRunningIndex) + fRunningIndex*PosY);
      }
    }
    
    /// <summary>
    /// Callback if the current job changes
    /// </summary>
    private void onCurrentJobChanged(object Sender, TValueChangedEventArgs<string> Args)
    {
      readWayPointsAsync(Args.Value).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Reads the new set of way points from vHub
    /// </summary>
    private async Task readWayPointsAsync(string strJobId)
    {
      if (strJobId.isNotEmpty())
      {
        TCommandArgs Args = new TCommandArgs();
        Args.Input["RouteId"] = strJobId;
        TCommandResult Result = TCommandHelper.call(ReadWayPointsCommand, Args);
        if (Result.State == EProcessingState.Good)
        {
          Logger.writeInfo($"Sync way points successful.");
          string strWayPoints = Args.getOutputArgument<string>("Content");
          CurrentProgram.passValue(strWayPoints);
          await calcWayAsync(JsonConvert.DeserializeObject<List<TMovementJM>>(strWayPoints));
        }
        else
        {
          Logger.writeError($"Failed to sync way points. {Result.ErrorMessage}");
        }
      }
    }
    
    /// <summary>
    /// Calculates the distance and time
    /// </summary>
    private async Task calcWayAsync(List<TMovementJM> Movements)
    {
      int iStep = 0;
      TMovementJM StartPos = new TMovementJM(CurrentPos.PosX, CurrentPos.PosY);
      
      //Calculates the distance to travel
      double fDist = 0;
      TMovementJM MovPrev = StartPos;
      foreach(TMovementJM Mov in Movements)
      {
        fDist += Math.Sqrt(Math.Pow(MovPrev.PosX - Mov.PosX, 2) + Math.Pow(MovPrev.PosY - Mov.PosY, 2));
        MovPrev = Mov;
      }
      SetTravelDistance.passValue(fDist);
      if (TFloat.isNonZero(SetSpeed.Value))
      {
        SetTravelTime.passValue(fDist/SetSpeed.Value/60.0);
      }
      await Task.CompletedTask;
      Logger.writeInfo($"Way points successful calculated Dist={fDist}m.");
    }
    
    /// <summary>
    /// runs the process
    /// </summary>
    private async Task runAsync(List<TMovementJM> Movements, CancellationToken Token)
    {
      int iStep = 0;
      TMovementJM StartPos = new TMovementJM(CurrentPos.PosX, CurrentPos.PosY);
      
      //Calculates the distance to travel
      await calcWayAsync(Movements);
      
      DateTime StartTime = DateTime.UtcNow;
      while(RunningState.Value == 1 && 
            iStep < Movements.Count && 
            BatteryLevel.Value > 1 &&
            !Token.IsCancellationRequested)
      {
        TMovementJM NextStep = Movements[iStep];
        double fRunningIndex = 0;
        
        //Store the last states (running meter and timestamp)
        double fLastRunningMeter = RunningMeter.Value;
        DateTime LastTime = DateTime.UtcNow;
  
        //Get the total length of travel (|v2 - v1| = |v|)
        double fTotalLength = new TMovementJM(NextStep.PosX - StartPos.PosX, NextStep.PosY - StartPos.PosY).Length;
        while(fTotalLength > 0 &&
              RunningState.Value == 1 && 
              fRunningIndex < 1.0 && 
              BatteryLevel.Value > 1 &&
              !Token.IsCancellationRequested)
        {
          DateTime LastTimeStep = DateTime.UtcNow;
          await Task.Delay(WaitingTime);

          //Calulate how far the robot traveled in the mean time
          // length = speed * time
          double fSeconds = (DateTime.UtcNow - LastTime).TotalSeconds;
          double fCurrentLength = SetSpeed.Value*fSeconds;
          
          //Get the running index for vector geometry
          fRunningIndex = fCurrentLength/fTotalLength;
          if (fRunningIndex > 1.0) { fRunningIndex = 1; }
          CurrentPos = NextStep.calNewPos(StartPos, fRunningIndex);

          //Set the metrics of the robot
          PositionX.passValue(CurrentPos.PosX);
          PositionY.passValue(CurrentPos.PosY);
          RunningMeter.passValue(fLastRunningMeter + fTotalLength*fRunningIndex);
          CurrentTravelTime.passValue((DateTime.UtcNow - StartTime).TotalSeconds/60.0);

          //Power consumption
          consumePower(SetSpeed.Value/1.01, (DateTime.UtcNow - LastTimeStep).TotalHours);
        }
        StartPos = NextStep;
        iStep++;
      }
      RunningState.passValue(0);
      //SetTravelDistance.passValue(0);
      //SetTravelTime.passValue(0);
    }
    
    /// <summary>
    /// Calculates the battery consumption
    /// </summary>
    private void consumePower(double fPowerConsumption, double fHours)
    {
      double fBatteryCapacity = BatteryCapacity.Value - fPowerConsumption*fHours; 
      if (fBatteryCapacity < 0) { fBatteryCapacity = 0; }

      BatteryCapacity.passValue(fBatteryCapacity);
      BatteryLevel.passValue(fBatteryCapacity/TotalBatteryCapacity * 100);
      PowerConsumption.passValue(fPowerConsumption);
    }
    
    private TMovementJM CurrentPos              { get; set; }
    private IDataNode<int> AlarmState           { get; set; }
    private IDataNode<int> RunningState         { get; set; }
    private IDataNode<double> PositionX         { get; set; }
    private IDataNode<double> PositionY         { get; set; }
    private IDataNode<double> SetSpeed          { get; set; }
    private IDataNode<double> RunningMeter      { get; set; }
    private IDataNode<double> BatteryCapacity   { get; set; }
    private IDataNode<double> BatteryLevel      { get; set; }
    private IDataNode<double> PowerConsumption  { get; set; }
    private IDataNode<string> CurrentProgram    { get; set; }
    private IDataNode<double> SetTravelDistance { get; set; }
    private IDataNode<double> SetTravelTime     { get; set; }
    private IDataNode<double> CurrentTravelTime { get; set; }
    private IDataNode<string> CurrentJobId      { get; set; }
    private ICommandNode ReadWayPointsCommand   { get; set; }
  }
}
