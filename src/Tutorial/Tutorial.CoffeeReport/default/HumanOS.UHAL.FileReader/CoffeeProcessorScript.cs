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

using HumanOS.Kernel.Processing;
using System.Text.RegularExpressions;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Blank script for processing
  /// </summary>
  public class CoffeeProcessingScriptObject : TAbstractProcessingScriptObject
  {
    ///<see cref="TAbstractProcessingScriptObject"/>
    public override void process(IProcessingNode Processor)
    {
      string strBrand = Processor.getProperty<string>("InBrand");
      string strName = Processor.getProperty<string>("InName");
      string strCapsuleNetWeight = Processor.getProperty<string>("InCapsuleNetWeight");
      string strCoffeeAmount = Processor.getProperty<string>("InCoffeeAmount");
      string strMilkAmount = Processor.getProperty<string>("InMilkAmount");
      string strStrength = Processor.getProperty<string>("InStrength");
      
      if (strBrand != null && 
          strCapsuleNetWeight != null &&
          strCoffeeAmount != null &&
          strMilkAmount != null &&
          strName != null &&
          strStrength != null)
      {
      
        string[] astrStrength = strStrength.Split("/");
      
        double fCapsuleNetWeight = getDoubleValue(strCapsuleNetWeight) / 1000.0;
        double fCoffeeAmount = getDoubleValue(strCoffeeAmount) / 1000.0;
        double fMilkAmount = getDoubleValue(strMilkAmount) / 1000.0;
        double fStrength = getDoubleValue(astrStrength[0]) / getDoubleValue(astrStrength[1]);
        double fTotal = fMilkAmount + fCoffeeAmount;

        Processor.setProperty<double>("OutCapsuleNetWeight", fCapsuleNetWeight);
        Processor.setProperty<double>("OutCoffeeAmount", fCoffeeAmount);
        Processor.setProperty<double>("OutMilkAmount", fMilkAmount);
        Processor.setProperty<double>("OutStrengthPercent", fStrength * 100.0);
        Processor.setProperty<double>("OutCoffeePercent", fCoffeeAmount / fTotal * 100);
        Processor.setProperty<double>("OutMilkPercent", fMilkAmount / fTotal * 100);
      }
    }
    
    /// gets a double value from a string
    private double getDoubleValue(string strValue)
    {
      double fRetval = double.NaN;
      Regex TakeValues = new Regex("^[\\d|\\.|\\,]+");
      string strLetterfree = TakeValues.Match(strValue).Value.Replace(",", ".");
      if (!string.IsNullOrEmpty(strLetterfree))
      {
        fRetval = double.Parse(strLetterfree);
      }
      return fRetval;
    }
  }
}
