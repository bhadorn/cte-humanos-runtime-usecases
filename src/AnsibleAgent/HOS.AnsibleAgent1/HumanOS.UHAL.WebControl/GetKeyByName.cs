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

using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.UHAL.Script;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example for Http payload parsing
  /// </summary>
  public class Blank_UHALHttpScriptObject : TAbstractHttpScriptObject
  {
    ///<see cref="TAbstractHttpScriptObject"/>
    public override string composeRequest(ref string rstrAddress, 
                                          Dictionary<string, string> dicProperties, 
                                          Dictionary<string, object> dicInputArguments,
                                          Dictionary<string, IEnumerable<string>> dicRequestHeaders)
    {
      dicRequestHeaders["Cookie"] = new List<string>()
      {
        TCommandHelper.getArgument<string>(dicInputArguments, "AuthCookie")
      };

      return "";
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override void parseResponse(string strAddress, Dictionary<string, string> dicProperties, string strContent, EContentType eContentType, Dictionary<string, object> dicInputArguments, Dictionary<string, object> dicOutputArguments)
    {
      try
      {
        string strKeyName = TCommandHelper.getArgument<string>(dicInputArguments, "Name");
        int iKeyId = 0;
        JArray jRoot = JArray.Parse(strContent);
        foreach(JObject jObj in jRoot)
        {
          if ((string)jObj["name"] == strKeyName)
          {
            iKeyId = (int)jObj["id"];
            break;
          }
        }
      
        if (iKeyId == 0)
        {
          throw new ArgumentException($"Key with name '{strKeyName}' not found.");
        }
        dicOutputArguments["KeyId"] = iKeyId;
      }
      catch(Exception Exc)
      {
        Logger.writeError($"{strContent}");
        throw;
      }
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override TCommandResult parseError(Dictionary<string, string> dicProperties, Dictionary<string, object> dicInputArguments, string strMethod, string strAddress, string strRequestBody, THttpResponse Response)
    {
      Logger.writeError(Response.Content);
      return base.parseError(dicProperties, dicInputArguments, strMethod, strAddress, strRequestBody, Response);
    }
  }
}
