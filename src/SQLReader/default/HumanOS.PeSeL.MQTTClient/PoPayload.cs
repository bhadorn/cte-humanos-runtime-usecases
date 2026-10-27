using HumanOS.Kernel;
using HumanOS.Kernel.PeSeL.DataLogger;
using HumanOS.Kernel.PeSeL.Script;
using System.Collections.Generic;
using System.Text;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  // Payload script of the publisher "PoPublisher".
  //
  // The publisher is event-based (SamplingRate 0): for every write to "PoStart" or "PoFinish"
  // this script is called with exactly one DataSet.
  //
  //   Topic   = <Topic from settings.json>/<name of the data node>, e.g. company/line1/PoStart
  //   Payload = order number as plain text, e.g. 12345
  public class TPoPayload : TAbstractDataLoggerScriptObject<byte[]>
  {
    ///<see cref="TAbstractDataLoggerScriptObject{T}"/>
    public override void initialize(IKernelAccess Kernel, TPayloadProcessingContext Context)
    {
    }

    ///<see cref="TAbstractDataLoggerScriptObject{T}"/>
    public override void postProcess(IKernelAccess Kernel, TPayloadProcessingContext Context)
    {
    }

    ///<see cref="TAbstractDataLoggerScriptObject{T}"/>
    public override byte[][] processPayload(IKernelAccess Kernel, TPayloadProcessingContext Context, List<TDataSet> lstData)
    {
      List<byte[]> lstRetval = new List<byte[]>();

      // The publisher reads the topic from the context once per call, so only the first DataSet
      // is processed here; in event-based mode there is always exactly one anyway.
      if (lstData.Count > 0)
      {
        string strTopicBase = Context.getValue<string>("DefaultTopic").TrimEnd('/');
        Context.setValue("Topic", $"{strTopicBase}/{lstData[0].getFieldValue<string>("Name")}");
        lstRetval.Add(Encoding.UTF8.GetBytes(lstData[0].getFieldValue<int>("Value").ToString()));
      }

      return lstRetval.ToArray();
    }
  }
}
