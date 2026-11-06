/*****************************************************************************
 * Command GetNextPage of the device GraphClient (Microsoft Graph).
 *
 * Graph returns a long list in pages and ends every page but the last with the absolute URL of the next
 * one ("@odata.nextLink"). This command calls exactly that URL (input argument Url), the answer has the
 * same shape as the one of GetCalendarView. The URL already holds the whole query, so nothing is added to it.
 *****************************************************************************/

using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  public class TGetNextPageScriptObject : TAbstractHttpScriptObject
  {
    ///<see cref="TAbstractHttpScriptObject"/>
    public override string composeRequest(ref string rstrAddress,
                                          Dictionary<string, string> dicProperties,
                                          Dictionary<string, object> dicInputArguments,
                                          Dictionary<string, IEnumerable<string>> dicRequestHeaders)
    {
      string strUrl = dicInputArguments.TryGetValue("Url", out object oUrl) ? oUrl as string : null;
      if (strUrl.isEmpty())
      {
        throw new ArgumentException("Missing input argument 'Url' or its value is empty.");
      }

      //the link is absolute and replaces the address of the command
      rstrAddress = strUrl;
      dicRequestHeaders["Prefer"] = new[] { "outlook.timezone=\"UTC\"" };
      return "";
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override TCommandResult parseError(Dictionary<string, string> dicProperties,
                                              Dictionary<string, object> dicInputArguments,
                                              string strMethod,
                                              string strAddress,
                                              string strRequestBody,
                                              THttpResponse Response)
    {
      //Graph explains an error in the body: {"error":{"code":"ErrorAccessDenied","message":"..."}}
      string strDetail = Response.ReasonPhrase;
      try
      {
        JToken Error = JObject.Parse(Response.Content ?? "").SelectToken("error");
        if (Error != null)
        {
          strDetail = $"{(string)Error["code"]}: {(string)Error["message"]}";
        }
      }
      catch (JsonException)
      {
        //not JSON, the reason phrase has to do
      }
      return new TCommandResult(EProcessingState.BadProcessing, $"HTTP {(int)Response.StatusCode} {strDetail}");
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
      dicOutputArguments["Content"] = strContent;
    }
  }
}
