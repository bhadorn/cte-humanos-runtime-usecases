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
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Utils;
using CyberTech;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Script for temperature alerts
  /// </summary>
  public class TAlertProcessingScript : TAbstractProcessingScriptObject
  {
    ///<see cref="TAbstractProcessingScriptObject"/>
    public override void process(IProcessingNode Processor)
    {
      double fTemperature = Processor.getProperty<double>("Temperature");
      
      Processor.setProperty<bool>("Alert50Out", Threshold50.checkThreshold(fTemperature, TDateTime.UtcNowHighRes));
      Processor.setProperty<bool>("Alert60Out", Threshold60.checkThreshold(fTemperature, TDateTime.UtcNowHighRes));
      Processor.setProperty<bool>("Alert70Out", Threshold70.checkThreshold(fTemperature, TDateTime.UtcNowHighRes));
    }
    
    public TThresholdCalculator Threshold50 { get; } = new TThresholdCalculator(new TThresholdSettings()
    {
      Exceed = 50,
      ExceedOn = true,
      Undercut = 48
    });
    
    public TThresholdCalculator Threshold60 { get; }= new TThresholdCalculator(new TThresholdSettings()
    {
      Exceed = 60,
      ExceedOn = true,
      Undercut = 58
    });
    
    public TThresholdCalculator Threshold70 { get; }= new TThresholdCalculator(new TThresholdSettings()
    {
      Exceed = 70,
      ExceedOn = true,
      Undercut = 68
    });
  }
}
