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

using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.UHAL.Script;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace HumanOS.UHAL.FanucControl.Scripts
{
  /// <summary>
  /// 
  /// </summary>
  public class TAlarmProcessor : TAbstractProcessingScriptObject
  {
    ///<see cref="TAbstractProcessingScriptObject"/>
    public override void process(IProcessingNode Processor)
    {
      //Reads all alarms 
      List<TAlarmItem> lstAlarms = Processor.getAllAlarmMessages("Alarms");
      
      //Converts all alarms into a json array
      string strData = JsonConvert.SerializeObject(lstAlarms);
      Logger.writeVerbose(strData);
      
      //Send the alarm list as string to the data node
      Processor.setProperty<string>("OutputPort", strData);
    }
  }
}
