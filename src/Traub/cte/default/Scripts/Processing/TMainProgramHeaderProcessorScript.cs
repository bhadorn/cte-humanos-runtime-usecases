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

using HumanOS.Kernel.Processing;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Blank script for processing
  /// </summary>
  public class BlankProcessingScriptObject : TAbstractProcessingScriptObject
  {
    ///<see cref="TAbstractProcessingScriptObject"/>
    public override void process(IProcessingNode Processor)
    {
      string strValue = Processor.getProperty<string>("In_MainProgramHeader");
      Processor.setProperty<string>("Out_ProductionStep", strValue);
      Processor.setProperty<string>("Out_ProductName", "Test Maschine");
    }
  }
}
