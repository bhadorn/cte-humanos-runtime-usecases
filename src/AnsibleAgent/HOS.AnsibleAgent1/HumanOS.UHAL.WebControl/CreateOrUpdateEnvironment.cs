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

using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.UHAL.Script;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example for Http payload parsing
  /// </summary>
  public class TCreateEnvironmentScript : TAbstractHttpScriptObject
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
      
      int iEnvironmentId = TCommandHelper.getArgumentOrDefault<int>(dicInputArguments, "EnvironmentId", 0);
      
      JObject jRoot = new JObject();
      jRoot.Add(new JProperty("name", dicInputArguments["Name"]));
      jRoot.Add(new JProperty("project_id", dicInputArguments["ProjectId"]));
      jRoot.Add(new JProperty("env", dicInputArguments["Content"]));
      jRoot.Add(new JProperty("json", "{}"));
      jRoot.Add(new JProperty("password", ""));
      
      if (iEnvironmentId > 0)
      {
        jRoot.Add(new JProperty("id", iEnvironmentId));
      }
      
      return jRoot.ToString();
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override void parseResponse(string strAddress, Dictionary<string, string> dicProperties, string strContent, EContentType eContentType, Dictionary<string, object> dicInputArguments, Dictionary<string, object> dicOutputArguments)
    {
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override TCommandResult parseError(Dictionary<string, string> dicProperties, Dictionary<string, object> dicInputArguments, string strMethod, string strAddress, string strRequestBody, THttpResponse Response)
    {
      Logger.writeError(Response.Content);
      return base.parseError(dicProperties, dicInputArguments, strMethod, strAddress, strRequestBody, Response);
    }
  }
}
