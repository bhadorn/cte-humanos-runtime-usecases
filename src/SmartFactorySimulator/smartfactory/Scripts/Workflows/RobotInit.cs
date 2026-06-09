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
using HumanOS.Kernel.Workflow.Activity;
using HumanOS.Kernel.Workflow.Instruction;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of a workflow operation script
  /// </summary>
  public class TRobotInit : TAbstractOperationScriptObject
  {
    ///<see cref="TAbstractOperationScriptObject"/>
    public override async Task runAsync(IKernelAccess Kernel, IActivity Activity, CancellationToken CancellationToken)
    {
      await Task.Delay(10000, CancellationToken);
      
      IGroupRelation nWorkflow = Activity.Context.getValue<object>("Workflow") as IGroupRelation;
      IGroupRelation nDevice = nWorkflow.Relations.First(n => n.hasProperty("DriverId")) as IGroupRelation;
        
      if (nDevice == null)
      {
        throw new ArgumentException($"No device registered in workflow context.");
      }
      IGroupRelation nController = nDevice.queryNodeLocally(n => n.Name == "Controller") as IGroupRelation;
      if (nController == null)
      {
        throw new ArgumentException($"Could not find controller of '{nDevice.Name}'.");
      }

      IGroupRelation nBattery = nDevice.queryNodeLocally(n => n.Name == "Battery") as IGroupRelation;
      if (nBattery == null)
      {
        throw new ArgumentException($"Could not find battery of '{nDevice.Name}'.");
      }

      IGroupRelation nTray = nDevice.queryNodeLocally(n => n.Name == "Tray") as IGroupRelation;
      if (nTray == null)
      {
        throw new ArgumentException($"Could not find tray of '{nDevice.Name}'.");
      }
      
      IGroupRelation nVHub = Kernel.NodeSpace.queryNodeLocally(n => n.Name == "vHub") as IGroupRelation;
      if (nTray == null)
      {
        throw new ArgumentException($"Could not find device 'vHub'.");
      }

      Logger.writeInfo($"Initializing the device '{nDevice.Name}'...");
      Activity.Context.setValue("Device",     nDevice);
      Activity.Context.setValue("Controller", nController);
      Activity.Context.setValue("Battery",    nBattery);
      Activity.Context.setValue("Tray",       nTray);
      Activity.Context.setValue("vHub",       nVHub);
      Logger.writeInfo($"Initializing done.");
      
      await Task.CompletedTask;
    }
  }
}
