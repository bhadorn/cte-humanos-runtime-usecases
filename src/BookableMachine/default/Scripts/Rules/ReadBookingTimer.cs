/*****************************************************************************
 * Rule script of the timer "Timer" in the DataModel object "BookingPolling".
 *
 * The timer fires 10 s after the start of the runtime and then every 15 s (see DataModel/Objects/BookingPolling.json).
 * The object must not have the same name as the command ReadBooking: the lookup by name would find the object.
 * Every tick starts the command ReadBooking. The same command can be started by any other trigger, for
 * example a workflow, or by an Agent of the HumanOS Platform: the trial runtime has no shared node space
 * and therefore no Platform, so a local trigger like this timer is needed.
 *****************************************************************************/

using CyberTech.Diagnostics;
using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Rules;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Utils;
using System;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  public class TReadBookingTimer : TAbstractRuleScriptObject
  {
    private const string COMMAND_NAME = "ReadBooking";

    ///<see cref="TAbstractRuleScriptObject"/>
    public override void execute(IKernelAccess Kernel, IVariablePool Pool)
    {
      try
      {
        ICommandNode Command = Kernel.NodeSpace.queryNodeLocally(n => n.Name == COMMAND_NAME) as ICommandNode;
        if (Command == null)
        {
          TLogger.writeError($"ReadBookingTimer: the command '{COMMAND_NAME}' was not found.");
          return;
        }

        TCommandResult Result = TCommandHelper.call(Command, new TCommandArgs());
        if (Result.State != EProcessingState.Good)
        {
          TLogger.writeError($"ReadBookingTimer: '{COMMAND_NAME}' failed: {Result.ErrorMessage}");
        }
      }
      catch (Exception Exc) when (Exc.isNotCancelException())
      {
        TLogger.writeError($"ReadBookingTimer: '{COMMAND_NAME}' threw an exception: {Exc.Message}");
      }
    }
  }
}
