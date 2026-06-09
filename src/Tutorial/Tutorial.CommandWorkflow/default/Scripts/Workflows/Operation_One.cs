/*****************************************************************************
* Copyright (C) by CyberTech Engineering 2022 – www.cybertech.swiss         *
*****************************************************************************
* Project: HumanOS (R)
* Date   : 2022
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
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of a workflow operation script
  /// </summary>
  public class TBlankPeMiLOperationScriptObject : TAbstractOperationScriptObject
  {
     ///<see cref="TAbstractOperationScriptObject"/>
     public override Task runAsync(IKernelAccess Kernel, IActivity Activity, CancellationToken CancellationToken)
     {
        try
        {
          if (!Activity.Context.hasValue("Arg1"))
          {
            throw new ArgumentException($"Missing argument 'Arg1', cannot start workflow.");
          }
          Logger.writeInfo($"Hello from workflow, with greetings text '{Activity.Context.getValue<string>("Arg1")}'");
        }
        catch (Exception Exc)
        {
          Logger.writeError($"Error occurred while executing workflow.", Exc);
        }
        return Task.CompletedTask;
     }
  }
}