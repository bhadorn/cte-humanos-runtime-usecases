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
using HumanOS.Kernel.UHAL.Script;
using System.Collections.Generic;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example for Http payload parsing
  /// </summary>
  public class TLoginScript : TAbstractHttpScriptObject
  {

    ///<see cref="TAbstractHttpScriptObject"/>
    public override string composeRequest(ref string rstrAddress, Dictionary<string, string> dicProperties, Dictionary<string, object> dicInputArguments)
    {
      return $@"{{ ""auth"": ""{dicProperties["UserName"]}"", ""password"" : ""{dicProperties["Password"]}"" }}";
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
      if (dicResponseHeaders.TryGetValue("Set-Cookie", out IEnumerable<string> Cookies))
      {
        foreach(string strCookie in Cookies)
        {
          Logger.writeVerbose($"Cookie: {strCookie}");
          if (strCookie.StartsWith("semaphore="))
          {
            dicOutputArguments["AuthCookie"] = strCookie;
          }
        }
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
