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
using CyberTech;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Blank script for processing
  /// </summary>
  public class TStorageSimulatorTemperatureProcessing : TAbstractProcessingScriptObject
  {
    ///<see cref="TAbstractProcessingScriptObject"/>
    public override void process(IProcessingNode Processor)
    {
      //double fValue = Processor.getProperty<double>("TempIn");

      double fRandomNoise = TRandom.getRandomFloat64() - 0.5;
      Processor.setProperty<double>("OutputPort", 20 + fRandomNoise);
    }
  }
}
