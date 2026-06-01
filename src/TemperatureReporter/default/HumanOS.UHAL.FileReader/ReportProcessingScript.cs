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

using HumanOS.Kernel.Processing;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// script for temperature processing
  /// </summary>
  public class TReportProcessingScript : TAbstractProcessingScriptObject
  {
    ///<see cref="TAbstractProcessingScriptObject"/>
    public override void process(IProcessingNode Processor)
    {
      double fTemperature = Processor.getProperty<double>("Temperature");
      
      Logger.writeInfo($"Temp. Report: {fTemperature}°C");
      Processor.setProperty<double>("Output", fTemperature);
    }
  }
}
