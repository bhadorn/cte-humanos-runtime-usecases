/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2022 � www.cybertech.swiss         *
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
using HumanOS.Kernel.Communication;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.InfoModel;
using HumanOS.Kernel.UHAL.InfoModel;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using System;
using System.Linq;

namespace HumanOS.UHAL.TcpClientControl
{
  /// <summary>
  /// Implement sthe DMG Mori Seiki native payload parsing.
  /// Connect to the machine using port 7878
  /// </summary>
  public class MoriSeikiPayloadProcessor : TAbstractStreamScriptObject
  {
    ///<see cref="TAbstractStreamScriptObject"/>
    public override void handleStream(IKernelAccess Kernel, TDeviceSchemaInfo DeviceInfo, IDataStream DataStream)
    {
      byte[] aui8Buffer = new byte[40000];
      DataStream.read(aui8Buffer, 0, 40000);
      string strText = System.Text.Encoding.ASCII.GetString(aui8Buffer);
      string[] astrParts = strText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

      foreach (string strLine in astrParts)
      {
        string[] astrTokens = strLine.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < astrTokens.Length - 1; i += 2)
        {
          string strName = astrTokens[i];
          string strValue = astrTokens[i + 1];

          if (strValue == "ON") { strValue = "True"; }
          else if (strValue == "OFF") { strValue = "False"; }
          else if (strValue == "AVAILABLE") { strValue = "True"; }
          else if (strValue == "UNAVAILABLE") { strValue = "False"; }

          setValue(Kernel.NodeSpace, DeviceInfo, strName, strValue);
        }
      }
    }

    /// <summary>
    /// sets a value to a data node
    /// </summary>
    /// <param name="NodeSpace">nodespace</param>
    /// <param name="DeviceInfo">device info model</param>
    /// <param name="strName"></param>
    /// <param name="strValue"></param>
    private void setValue(INodeSpace NodeSpace, TDeviceSchemaInfo DeviceInfo, string strName, string strValue)
    {
      TDataNodeInfo nDataNodeInfo = DeviceInfo.DataNodes.FirstOrDefault(n => n.Name == strName);

      if (nDataNodeInfo != null)
      {
        if (NodeSpace.tryGetNode<IDataNode>(nDataNodeInfo.Id, out IDataNode DataNode))
        {
          Type tDataType = Type.GetType(nDataNodeInfo.DataType);
          TSimpleVariant Value = new TSimpleVariant(TValueConverter.convertToObject(tDataType, strValue));
          DataNode.passValue(Value, false, EDataState.Good);
        }
        else
        {
          Logger.writeWarning($"Node '{nDataNodeInfo.Id}' not configured.");
        }
      }
    }
  }
}
