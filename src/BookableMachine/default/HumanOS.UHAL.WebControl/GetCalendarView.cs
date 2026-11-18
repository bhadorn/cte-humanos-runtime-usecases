/*****************************************************************************
 * Command GetCalendarView of the device GraphClient (Microsoft Graph).
 *
 * GET users/{UserId}/calendar/calendarView?startDateTime={From}&endDateTime={To}&$top={PageSize}&...
 * The address of the command (device template) holds the whole URL, the script only adds the header
 * "Prefer: outlook.timezone" so that Graph returns every time in UTC, and passes the answer on.
 *****************************************************************************/

using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  public class TGetCalendarViewScriptObject : TAbstractHttpScriptObject
  {
    ///<see cref="TAbstractHttpScriptObject"/>
    public override string composeRequest(ref string rstrAddress,
                                          Dictionary<string, string> dicProperties,
                                          Dictionary<string, object> dicInputArguments,
                                          Dictionary<string, IEnumerable<string>> dicRequestHeaders)
    {
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
