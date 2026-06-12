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

using CyberTech;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Script;
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.Workflow;
using HumanOS.Kernel.Workflow.Activity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of a command script
  /// </summary>
  public class TBlankPeMiLCommandScriptObject : TAbstractCommandScriptObject
  {
     ///<see cref="TAbstractCommandScriptObject"/>
     public override async Task<TCommandResult> executeCommandAsync(ICommandNode CommandNode,
                                                                    Dictionary<string, object> dicInputArguments,
                                                                    Dictionary<string, object> dicOutputArguments,
                                                                    CancellationToken Token)
     {
        TCommandResult Retval = new TCommandResult();

        try
        {
          if (!TCommandHelper.tryGetArgument<string>(dicInputArguments, "Arg1", out string strArg1))
          {
            throw new ArgumentException($"Missing or invalid type of input argument 'Arg1'.");
          }

          string strWorkflowName = "Workflow";
          Retval.setErrorInfo(EProcessingState.GoodInProgress);
          INodeSpace NodeSpace = (INodeSpace)CommandNode.Relations.First(n => n is INodeSpace);
          foreach (INode Device in NodeSpace.queryNodesLocally(n => n.Name != ""))
          {
            IGroupRelation nDevice = Device as IGroupRelation;
            if (nDevice != null && nDevice.hasProperty("DriverId"))
            {
              bool bResult = TObject.castAndExecute<IWorkflow>(nDevice.queryNodeLocally(n => n.Name == strWorkflowName), (o) =>
              {
                if (o.State == EActivityState.Running)
                {
                  throw new ArgumentException($"Cannot execute the action, device is busy, try again later.");
                }
                else
                {
                  TCommandCallContext Ccc = o.createCallContext();
                  Ccc.setInputArgumentValue<string>("Arg1", strArg1);
                  TCommandResult Res = o.execute(Ccc);
                  if (Res.State != EProcessingState.Good)
                  {
                      throw new ArgumentException($"Unable to start workflow for device '{nDevice.Name}'.", o.Context.ErrorInfo);
                  }
                }
              });
              if (!bResult)
              {
                Logger.writeWarning($"Did not find workflow with name '{strWorkflowName}' in node '{nDevice.Name}'.");
              }
            }
          }
        }
        catch (Exception Exc)
        {
          Retval.setErrorInfo(EProcessingState.BadProcessing, Exc);
        }

        return await Task.FromResult(Retval).ConfigureAwait(false);
     }
  }
}