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
using HumanOS.Kernel.PeSeL.DataLogger;
using HumanOS.Kernel.PeSeL.Script;
using HumanOS.Kernel.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example script for data loggers
  /// </summary>
  public class TBlankPeSeLBinaryDataLoggerScriptObject : TAbstractDataLoggerScriptObject<byte[]>
  {
    ///<see cref="TAbstractDataLoggerScriptObject{T}"/>
    public override void initialize(IKernelAccess Kernel,
                                    TPayloadProcessingContext Context)
    {
    }

    ///<see cref="TAbstractDataLoggerScriptObject{T}"/>
    public override void postProcess(IKernelAccess Kernel,
                                     TPayloadProcessingContext Context)
    {
    }

    ///<see cref="TAbstractDataLoggerScriptObject{T}"/>
    public override byte[][] processPayload(IKernelAccess Kernel, TPayloadProcessingContext Context, List<TDataSet> lstData)
	{
	  List<byte[]> lstRetval = new List<byte[]>();
	  if (lstData.Count > 0)
	  {
	    string strTopicName = $"mazak/data/{lstData[0].getFieldValue<string>("Name")}";
	    Context.setValue("Topic", strTopicName);
	  }
	   foreach(TDataSet DataSet in lstData)
	  {
	    JObject jRoot = new JObject();
	    jRoot.Add("DeviceId", DataSet.getFieldValue<Guid>("DeviceId"));
	    jRoot.Add("Id", DataSet.getFieldValue<Guid>("Id"));
	    jRoot.Add("State", DataSet.getFieldValue<int>("State"));
	    jRoot.Add("TimeStamp", DataSet.getFieldValue<DateTime>("TimeStamp").ToBinary());
	    jRoot.Add("DataType", DataSet.getFieldValue<Type>("DataType").FullName);
	    jRoot.Add("Name", DataSet.getFieldValue<string>("Name"));
	    jRoot.Add("Value", TValueConverter.convertToString(DataSet.Fields.First(n => n.Name == "Value").Value));
	    lstRetval.Add(Encoding.UTF8.GetBytes(jRoot.ToString()));
	  }
	
	  return lstRetval.ToArray();
	}
  }
}
