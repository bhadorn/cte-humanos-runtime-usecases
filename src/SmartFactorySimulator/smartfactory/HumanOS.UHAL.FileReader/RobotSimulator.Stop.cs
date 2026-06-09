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
using HumanOS.Kernel.UHAL.Script;
using System.Collections.Generic;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example for UHAL logic
  /// </summary>
  public class TBlankUhalLogicScriptObject : TAbstractLogicScriptObject
  {
    ///<see cref="TAbstractLogicScriptObject"/>
    public override void executeCommand(IKernelAccess Kernel,
                                        IGroupRelation DeviceNode,
                                        Dictionary<string, string> dicProperties,
                                        Dictionary<string, object> dicInputArguments,
                                        Dictionary<string, object> dicOutputArguments)
    {
      Logger.writeInfo($"Stop Robot '{DeviceNode.Name}'.");
    }
  }
}
