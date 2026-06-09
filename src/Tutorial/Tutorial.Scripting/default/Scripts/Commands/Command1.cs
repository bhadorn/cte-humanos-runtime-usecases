/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2026 – www.cybertech.swiss         *
 *****************************************************************************
 * Project: HumanOS (R)
 * Date   : 2026
 *****************************************************************************
 * License:                                                                  *
 *   This library is protected software; you are not allowed to redistribute *
 *   whole or part of it to other companies or external persons without the  *
 *   authorization of the CEO CyberTech Engineering GmbH.                    *
 *****************************************************************************/

using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Script;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Examble of a command script
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
      Logger.writeInfo($"Hello from global command, received '{dicInputArguments["Arg1"]}' as input data.");
      return await Task.FromResult(Retval).ConfigureAwait(false);
    }
  }
}
