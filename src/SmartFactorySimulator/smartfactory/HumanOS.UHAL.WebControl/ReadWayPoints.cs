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

using CyberTech;
using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.UHAL.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example for Http payload parsing
  /// </summary>
  public class TReadWayPoints : TAbstractHttpScriptObject
  {
    ///<see cref="TAbstractHttpScriptObject"/>
    public override string composeRequest(ref string rstrAddress,
                                          Dictionary<string, string> dicProperties,
                                          Dictionary<string, object> dicInputArguments,
                                          Dictionary<string, IEnumerable<string>> dicRequestHeaders)
    {
      return "RequestPayload";
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override TCommandResult parseError(Dictionary<string, string> dicProperties,
                                              Dictionary<string, object> dicInputArguments,
                                              string strMethod,
                                              string strAddress,
                                              string strRequestBody,
                                              THttpResponse Response)
    {
      //TODO handle your errors here
      return base.parseError(dicProperties, dicInputArguments, strMethod, strAddress, strRequestBody, Response);
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override void parseResponse(string strAddress,
                                       Dictionary<string, string> dicProperties,
                                       string strContent,
                                       EContentType eContentType,
                                       Dictionary<string, IEnumerable<string>> dicResponseHeaders,
                                       Dictionary<string, object> dicInputArguments,
                                       Dictionary<string, object> dicOutputArguments)
    {
      string strRouteId = TCommandHelper.getArgumentOrDefault<string>(dicInputArguments, "RouteId", "");
      JArray jArray = JArray.Parse(strContent);
      JArray jOrderedArray = new JArray(jArray.OrderBy(n => n["waypointIndex"].ToString().toInt()));
      
      JArray jWaiPoints = new JArray();
      
      foreach(JObject jEntry in jOrderedArray)
      {
        if (strRouteId.isEmpty() || (string)jEntry["pathName"] == strRouteId)
        {
          JObject jWayPoint = new JObject();
          jWayPoint["Index"] = ((string)jEntry["waypointIndex"]).toInt();
          jWayPoint["PosX"] = ((string)jEntry["waypointX"]).toFloat();
          jWayPoint["PosY"] = ((string)jEntry["waypointZ"]).toFloat();
          //jWayPoint["PosZ"] = ((string)jEntry["Y"]).toFloat();
          jWaiPoints.Add(jWayPoint);
        }
      }
      
      TCommandHelper.setArgument(dicOutputArguments, "Content", jWaiPoints.ToString());
    }
  }
}
