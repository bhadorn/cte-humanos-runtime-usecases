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

using HumanOS.Kernel;
using HumanOS.PeSeL.McpServer.Script;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of a HumanOS MCP tool script. The returned text is handed back to the AI model as the
  /// tool result (plain text or JSON).
  /// </summary>
  public class TBlankPeSeLMcpScriptObject : TAbstractMcpScriptObject
  {
    ///<see cref="TAbstractMcpScriptObject"/>
    public override async Task<string> executeAsync(IKernelAccess Kernel,
                                                    TMcpToolCallContext Context,
                                                    CancellationToken Token)
    {
      await Task.CompletedTask.ConfigureAwait(false);
      return "Not implemented.";
    }
  }
}
