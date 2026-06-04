/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2021 – www.cybertech.swiss         *
 *****************************************************************************
 * Project: HumanOS (R)
 * Date   : 2021
 *****************************************************************************
 * License:                                                                  *
 *   This library is protected software; you are not allowed to redistribute *
 *   whole or part of it to other companies or external persons without the  *
 *   authorization of the CEO CyberTech Engineering GmbH.                    *
 *****************************************************************************/

using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HOS.Tooling.ToolExpert.Config.HumanOS.PeSeL.WebService
{
  /// <summary>
  /// Scripts to query DDI objects
  /// </summary>
  public class ToolExpert_ReadToolInfo : TAbstractHttpScriptObject
  {

    ///<see cref="TAbstractHttpScriptObject"/>
    public override string composeRequest(ref string strAddress,
                                          Dictionary<string, string> dicProperties,
                                          Dictionary<string, object> dicInputArguments)
    {
      return "";
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override void parseResponse(string strAddress,
                                       Dictionary<string, string> dicProperties,
                                       string strContent,
                                       EContentType eContentType,
                                       Dictionary<string, object> dicInputArguments,
                                       Dictionary<string, object> dicOutputArguments)
    {
      try
      {
        JObject jRootObj = JObject.Parse(strContent);

        JArray najProducts = jRootObj["products"] as JArray;
        if (najProducts != null && najProducts.Any())
        {
          Dictionary<string, object> dicValues = new Dictionary<string, object>();
          
          foreach (KeyValuePair<string, JToken> Property in (JObject)najProducts[0])
          {
            dicValues[Property.Key] = Property.Value; 
          }

          TCommandHelper.setArgument<string>(dicOutputArguments, "Content", JsonConvert.SerializeObject(dicValues));
        }
        else
        {
          throw new ArgumentException("No tool found.");
        }
      }
      catch (ThreadInterruptedException) { throw; }
      catch (ThreadAbortException) { throw; }
      catch (JsonReaderException Exc)
      {
        Logger.writeError($"Could not read tool information. {Exc.Message}");
        throw new ArgumentException("Could not read tool information.", Exc);
      }
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override TCommandResult parseError(Dictionary<string, string> dicProperties, Dictionary<string, object> dicInputArguments, string strMethod, string strAddress, string strRequestBody, THttpResponse Response)
    {
      TCommandResult Retval = new TCommandResult();
      string strContent = Response.Content;
      try
      {
        JObject jRootObj = JObject.Parse(strContent);

        string strMessage = (string)jRootObj["message"];
        if (strMessage.isNotEmpty())
        {
          Retval.setErrorInfo(EProcessingState.BadArguments, new ArgumentException(strMessage));
        }
        else
        {
          Retval = base.parseError(dicProperties, dicInputArguments, strMethod, strAddress, strRequestBody, Response);
        }
      }
      catch (Exception Exc) when (Exc.isNotCancelException())
      {
        Logger.writeError($"Could not read tool information. {Exc.Message}");
        Retval = base.parseError(dicProperties, dicInputArguments, strMethod, strAddress, strRequestBody, Response);
      }

      return Retval;
    }

    /// <summary>
    /// Converts a jToken to a double value
    /// </summary>
    /// <param name="jToken"></param>
    /// <returns></returns>
    private static double convertToDouble(JToken jToken)
    {
      return ((string)jToken).toFloat(0);
    }

    /// <summary>
    /// Converts a jToken to a int value
    /// </summary>
    /// <param name="jToken"></param>
    /// <returns></returns>
    private static int convertToInt(JToken jToken)
    {
      return ((string)jToken).toInt(0);
    }
  }
}
