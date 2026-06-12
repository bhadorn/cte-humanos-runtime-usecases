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
using HumanOS.Kernel.Workflow.Activity;
using HumanOS.Kernel.Workflow.Instruction;
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
        Kernel.postFatalError(Activity.Context.ErrorInfo);
        return Task.CompletedTask;
     }
  }
}