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

using CyberTech.Diagnostics;
using HumanOS.Kernel;
using HumanOS.Kernel.Communication;
using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.UHAL.Device;
using HumanOS.Kernel.UHAL.InfoModel;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example to handle HTTP streams
  /// </summary>
  public class MTConnectPayloadProcessor : TAbstractHttpStreamScriptObject
  {
    ///<see cref="TAbstractHttpStreamScriptObject"/>
    public override void handleStream(IKernelAccess Kernel, TDeviceSchemaInfo DeviceInfo, IHttpStream DataStream)
    {
      THttpResponse Response = DataStream.request("current", "GET", "", "text/xml", new Dictionary<string, string>());
      try
      {
        IDataNode<string> Node1 = Kernel.NodeSpace.getNode<IDataNode<string>>(DeviceInfo.DataNodes.First(n => n.Name == "OperationMode").Id);
        IDataNode<int> Node2 = Kernel.NodeSpace.getNode<IDataNode<int>>(DeviceInfo.DataNodes.First(n => n.Name == "PartCounter").Id);
        //TLogger.writeInfo(Response.Content);

        XDocument Doc = XDocument.Parse(Response.Content);
        XmlNamespaceManager Manager = new XmlNamespaceManager(Doc.CreateReader().NameTable);
        Manager.AddNamespace("m", "urn:mtconnect.org:MTConnectStreams:2.0");

        //Set the Operation Mode as String
        Node1.passValue(Doc.XPathSelectElement("//m:*[@dataItemId='mode']", Manager)?.Value, false);
        //Set the Part Count Act as Double
        string strPartCountAct = Doc.XPathSelectElement("//m:*[@dataItemId='PartCountAct']", Manager)?.Value; 
        Node2.passValue(strPartCountAct != null ? strPartCountAct.toInt(0) : 0, false);
      }
      catch (Exception Exc)
      {
        Logger.writeError($"Failed to read data. {Exc.Message}");
      }
    }
  }
}
