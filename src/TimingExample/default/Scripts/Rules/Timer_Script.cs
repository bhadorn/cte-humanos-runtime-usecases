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
using HumanOS.Kernel.DataModel.Rules;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Utils;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of a script for rules
  /// </summary>
  public class TBlankPeMiLRuleScriptObject : TAbstractRuleScriptObject
  {
    ///<see cref="TAbstractRuleScriptObject"/>
    public override void execute(IKernelAccess Kernel, IVariablePool Pool)
    {
      Logger.writeInfo("---------------------Timer executed------------------");
      
      //Lesen aller Devices
      foreach(IGroupRelation Device in Kernel.NodeSpace.queryNodes(n => n.hasProperty("DriverId")))
      {
        TCommandArgs Args = new TCommandArgs();
        Args.Input["Name"] = "..\\..\\..\\..\\..\\..\\File01.txt";
        TCommandResult Result = TCommandHelper.call(Device, "ReadFile", Args);
        if (Result.State == EProcessingState.Good)
        {
          Logger.writeInfo($"File of '{Device.Name}' read.");
        }
        else
        {
          Logger.writeError($"Failed to read file. {Result.ErrorMessage}");
        }
      }
    }
  }
}
