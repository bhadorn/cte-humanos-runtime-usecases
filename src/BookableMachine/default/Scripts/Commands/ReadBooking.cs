/*****************************************************************************
 * Command ReadBooking (Bookable Machine example).
 *
 * Reads the calendar of the machine mailbox (an Exchange equipment or room mailbox) through
 * Microsoft Graph (users/{id}/calendar/calendarView, read only), publishes the current and the
 * next booking, and compares the booking with the state of the FANUC controller.
 *
 * Needs three devices in the node space, found by their property "Endpoint":
 *   Endpoint = Exchange  the WebControl device "GraphClient" (commands GetCalendarView, GetNextPage)
 *   Endpoint = Calendar  the NoDevice device "MachineBooking" (RAM only, carries the OPC-UA nodes);
 *                        its property ResourceId is the object id of the machine mailbox
 *   Endpoint = Machine   the FanucControl device "Machine"; the node Controller/RunningState is read
 *                        (optional: without it the usage is "MachineUnknown")
 *
 * Usage (node "Usage" of MachineBooking):
 *   Free                  not booked, machine does not run
 *   BookedIdle            booked, machine does not run (set-up, break)
 *   InUse                 booked, machine runs
 *   RunningWithoutBooking not booked, machine runs        -> Mismatch = true
 *   MachineUnknown        the controller is not readable (not connected)
 *   BookingUnknown        the calendar could not be read (see Status)
 *****************************************************************************/

using CyberTech.Diagnostics;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Script;
using HumanOS.Kernel.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Reads the booking calendar of the machine and derives the usage of the machine from it.
  /// </summary>
  public class ReadBooking : TAbstractCommandScriptObject2
  {
    private const string ENDPOINT_GRAPH = "Exchange";
    private const string ENDPOINT_CALENDAR = "Calendar";
    private const string ENDPOINT_MACHINE = "Machine";
    private const string DATE_FORMAT = "yyyy-MM-ddTHH:mm:ss'Z'";
    private const int MAX_PAGES = 100;

    // FANUC running state (Nc1.Dynamic.Float64:5): 0 RESET, 1 STOP, 2 HOLD, 3 START (program running)
    private const int FANUC_RUNNING = 3;

    // One run at a time: if Graph is slow, the next timer tick must not overtake the running one.
    private static readonly SemaphoreSlim s_Gate = new SemaphoreSlim(1, 1);

    // OPC-UA has no empty date: an unused time node shows the OPC-UA zero time.
    private static readonly DateTime s_EmptyTime = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    ///<see cref="TAbstractCommandScriptObject2"/>
    public override async Task<TCommandResult> executeCommandAsync(ICommandCallContext CallContext,
                                                                  CancellationToken Token)
    {
      TCommandResult Retval = new TCommandResult();
      if (!await s_Gate.WaitAsync(0).ConfigureAwait(false))
      {
        Logger.writeInfo("The previous run is still active, skipped.");
        return Retval;
      }

      try
      {
        Logger.writeInfo("Started.");

        //find the three devices, the same way for every command script
        INodeSpace NodeSpace = (INodeSpace)CallContext.CallingNode.Relations.FirstOrDefault(n => n is INodeSpace) ??
          throw new ArgumentException("Unable to find the node space.");
        IGroupRelation GraphDevice = findDevice(NodeSpace, ENDPOINT_GRAPH);
        IGroupRelation BookingDevice = findDevice(NodeSpace, ENDPOINT_CALENDAR);
        //the machine device is allowed to be missing (driver not loaded, no FANUC library): the usage is then 'MachineUnknown'
        IGroupRelation MachineDevice = NodeSpace.queryNodeLocally(n => n.hasProperty("Endpoint", ENDPOINT_MACHINE)) as IGroupRelation;
        if (MachineDevice == null)
        {
          Logger.writeWarning($"Unable to find the device with the property 'Endpoint' = '{ENDPOINT_MACHINE}', the usage of the machine is unknown.");
        }

        //settings on the booking device
        int iLookAheadHours = Math.Max(1, BookingDevice.getProperty<int>("LookAheadHours", 48));
        int iPageSize = Math.Max(1, BookingDevice.getProperty<int>("PageSize", 50));
        bool bMailAddresses = BookingDevice.getProperty<bool>("IncludeMailAddresses", false);
        string strResourceId = BookingDevice.getProperty<string>("ResourceId", null);

        DateTime Now = DateTime.UtcNow;
        List<TBookingEvent> lstEvents = null;
        if (strResourceId.isEmpty() || strResourceId.StartsWith("<"))
        {
          setValue(BookingDevice, "Status", "Not configured: the property ResourceId of the device MachineBooking is empty or still a placeholder");
          Logger.writeInfo("No ResourceId configured, the calendar is not read.");
        }
        else
        {
          try
          {
            lstEvents = await readCalendarAsync(GraphDevice, strResourceId, Now, Now.AddHours(iLookAheadHours), iPageSize, Token).ConfigureAwait(false);
            publishBooking(BookingDevice, lstEvents, bMailAddresses);
            Logger.writeInfo($"Calendar: {lstEvents.Count} booking(s).");
          }
          catch (Exception Exc) when (Exc.isNotCancelException())
          {
            //keep the booking nodes of the last good run, only the status tells that they are old
            lstEvents = null;
            setValue(BookingDevice, "Status", $"Error: {Exc.Message}");
            Logger.writeError($"Reading the calendar failed. {Exc.Message}");
          }
        }

        publishUsage(BookingDevice, MachineDevice, lstEvents, Now);
        setValue(BookingDevice, "LastRun", DateTime.UtcNow);
        Logger.writeInfo("Completed.");
      }
      catch (Exception Exc) when (Exc.isNotCancelException())
      {
        Logger.writeError("Failed.", Exc);
        Retval.setErrorInfo(EProcessingState.BadProcessing, Exc);
      }
      finally
      {
        s_Gate.Release();
      }

      return Retval;
    }

    private static IGroupRelation findDevice(INodeSpace NodeSpace, string strEndpoint)
    {
      return (IGroupRelation)NodeSpace.queryNodeLocally(n => n.hasProperty("Endpoint", strEndpoint)) ??
        throw new ArgumentException($"Unable to find the device with the property 'Endpoint' = '{strEndpoint}'.");
    }

    /// <summary>
    /// Reads all events of the mailbox in the window, following '@odata.nextLink' until the last page.
    /// </summary>
    private async Task<List<TBookingEvent>> readCalendarAsync(IGroupRelation GraphDevice, string strResourceId, DateTime From, DateTime To, int iPageSize, CancellationToken Token)
    {
      List<TBookingEvent> lstRetval = new List<TBookingEvent>();

      TCommandArgs Args = new TCommandArgs();
      Args.Input["UserId"] = Uri.EscapeDataString(strResourceId);
      Args.Input["From"] = From.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
      Args.Input["To"] = To.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
      Args.Input["PageSize"] = iPageSize.ToString(CultureInfo.InvariantCulture);
      string strContent = await callAsync(GraphDevice, "GetCalendarView", Args, Token).ConfigureAwait(false);

      int iPage = 1;
      while (true)
      {
        JObject Page = JObject.Parse(strContent);
        if (!(Page["value"] is JArray Values))
        {
          throw new InvalidOperationException("The answer of Graph has no 'value' array.");
        }
        foreach (JToken Item in Values)
        {
          TBookingEvent Event = TBookingEvent.parse((JObject)Item, Logger);
          if (!Event.IsCancelled)
          {
            lstRetval.Add(Event);
          }
        }

        //Graph limits the page size: the next page is only available through the link of the answer
        string strNext = (string)Page["@odata.nextLink"];
        if (strNext.isEmpty())
        {
          break;
        }
        if (++iPage > MAX_PAGES)
        {
          Logger.writeWarning($"More than {MAX_PAGES} pages, the remaining events are ignored. Increase PageSize.");
          break;
        }
        Logger.writeDebug($"Reading page {iPage}...");
        TCommandArgs NextArgs = new TCommandArgs();
        NextArgs.Input["Url"] = strNext;
        strContent = await callAsync(GraphDevice, "GetNextPage", NextArgs, Token).ConfigureAwait(false);
      }

      return lstRetval.OrderBy(n => n.Start).ThenBy(n => n.End).ToList();
    }

    /// <summary>
    /// Calls a command of the Graph device and returns its output 'Content'.
    /// </summary>
    private async Task<string> callAsync(IGroupRelation Device, string strCommandName, TCommandArgs Args, CancellationToken Token)
    {
      ICommandNode Node = (ICommandNode)Device.queryNodeLocally(n => n.Name == strCommandName) ??
        throw new ArgumentException($"Unable to find the command '{strCommandName}' in device '{Device.Name}'.");
      Token.ThrowIfCancellationRequested();
      TCommandResult Result = await Task.Run(() => TCommandHelper.call(Node, Args)).ConfigureAwait(false);
      if (Result.State != EProcessingState.Good)
      {
        throw new InvalidOperationException($"{strCommandName} failed: {Result.ErrorMessage}");
      }
      return Args.getOutputArgument<string>("Content", null) ??
        throw new InvalidOperationException($"{strCommandName} returned no content.");
    }

    /// <summary>
    /// Writes the current and the next booking and the JSON list of all bookings.
    /// </summary>
    private void publishBooking(IGroupRelation BookingDevice, List<TBookingEvent> lstEvents, bool bMailAddresses)
    {
      DateTime Now = DateTime.UtcNow;
      TBookingEvent Current = lstEvents.FirstOrDefault(n => n.Start <= Now && n.End > Now);
      TBookingEvent Next = lstEvents.FirstOrDefault(n => n.Start > Now);

      setValue(BookingDevice, "EventCount", lstEvents.Count);
      setValue(BookingDevice, "Booked", Current != null);
      setValue(BookingDevice, "BookedBy", Current?.OrganizerName ?? "");
      setValue(BookingDevice, "BookedSubject", Current?.Subject ?? "");
      setValue(BookingDevice, "BookedUntil", Current?.End ?? s_EmptyTime);
      setValue(BookingDevice, "NextStart", Next?.Start ?? s_EmptyTime);
      setValue(BookingDevice, "NextBookedBy", Next?.OrganizerName ?? "");
      setValue(BookingDevice, "Events", JsonConvert.SerializeObject(new JArray(lstEvents.Select(n => n.toJson(bMailAddresses))), Formatting.None));
      setValue(BookingDevice, "LastUpdate", DateTime.UtcNow);
      setValue(BookingDevice, "Status", "OK");
    }

    /// <summary>
    /// Compares the booking with the running state of the controller and writes MachineRunning, Usage and Mismatch.
    /// 'lstEvents' is null when the calendar could not be read.
    /// </summary>
    private void publishUsage(IGroupRelation BookingDevice, IGroupRelation MachineDevice, List<TBookingEvent> lstEvents, DateTime Now)
    {
      bool bMachineKnown = tryReadRunning(MachineDevice, out bool bRunning);
      setValue(BookingDevice, "MachineRunning", bRunning);

      string strUsage;
      if (lstEvents == null)
      {
        strUsage = "BookingUnknown";
      }
      else if (!bMachineKnown)
      {
        strUsage = "MachineUnknown";
      }
      else
      {
        bool bBooked = lstEvents.Any(n => n.Start <= Now && n.End > Now);
        strUsage = bBooked ? (bRunning ? "InUse" : "BookedIdle") : (bRunning ? "RunningWithoutBooking" : "Free");
      }

      setValue(BookingDevice, "Usage", strUsage);
      setValue(BookingDevice, "Mismatch", strUsage == "RunningWithoutBooking");
      Logger.writeInfo($"Usage: {strUsage}.");
    }

    /// <summary>
    /// Reads the node Controller/RunningState of the machine from the value the driver delivered last.
    /// Returns false when the controller is not readable (not connected, node missing).
    /// </summary>
    private bool tryReadRunning(IGroupRelation MachineDevice, out bool bRunning)
    {
      bRunning = false;
      IGroupRelation Controller = MachineDevice?.queryNodeLocally(n => n.Name == "Controller") as IGroupRelation;
      IDataNode<int> State = Controller?.queryNodeLocally(n => n.Name == "RunningState") as IDataNode<int>;
      if (State == null)
      {
        if (MachineDevice != null)
        {
          Logger.writeWarning("The machine device has no node Controller/RunningState of type Int32.");
        }
        return false;
      }

      EDataState eState = State.requestValue(out int iState, false);
      if (eState != EDataState.Good)
      {
        Logger.writeInfo($"The state of the machine is not readable ({eState}).");
        return false;
      }

      bRunning = iState == FANUC_RUNNING;
      return true;
    }

    /// <summary>
    /// Sets the value of the data node with the given name inside a group (a device is a group, too).
    /// The nodes are read only for OPC-UA clients; 'bInvokeWriteCommand = false' marks the value as coming
    /// from the source (here: this script) and not as a write from outside.
    /// </summary>
    private static void setValue<T>(IGroupRelation Group, string strName, T Value)
    {
      IDataNode<T> Node = Group.queryNodeLocally(n => n.Name == strName) as IDataNode<T> ??
        throw new InvalidOperationException($"'{Group.Name}' has no data node '{strName}' of type {typeof(T).Name}.");
      Node.passValue(Value, false);
    }
  }

  /// <summary>
  /// An event of the machine calendar (only the fields the example publishes).
  /// </summary>
  public class TBookingEvent
  {
    public string Id { get; private set; }
    public string Subject { get; private set; }
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public bool IsAllDay { get; private set; }
    public bool IsCancelled { get; private set; }
    public string OrganizerName { get; private set; }
    public string OrganizerMail { get; private set; }

    /// <summary>
    /// Reads one element of the Graph answer. Missing fields stay empty, a missing start or end is an error.
    /// </summary>
    public static TBookingEvent parse(JObject Item, ILogger Logger)
    {
      return new TBookingEvent
      {
        Id = (string)Item["id"] ?? "",
        Subject = (string)Item["subject"] ?? "",
        IsAllDay = (bool?)Item["isAllDay"] ?? false,
        IsCancelled = (bool?)Item["isCancelled"] ?? false,
        Start = parseTime(Item["start"], Logger),
        End = parseTime(Item["end"], Logger),
        OrganizerName = (string)Item.SelectToken("organizer.emailAddress.name") ?? "",
        OrganizerMail = (string)Item.SelectToken("organizer.emailAddress.address") ?? ""
      };
    }

    /// <summary>
    /// The JSON of one event as published in the node 'Events'. The mail address only on request.
    /// </summary>
    public JObject toJson(bool bMailAddresses)
    {
      JObject Organizer = new JObject { ["name"] = OrganizerName };
      if (bMailAddresses)
      {
        Organizer["mail"] = OrganizerMail;
      }
      return new JObject
      {
        ["id"] = Id,
        ["subject"] = Subject,
        ["start"] = Start.ToString(DATE_FORMAT, CultureInfo.InvariantCulture),
        ["end"] = End.ToString(DATE_FORMAT, CultureInfo.InvariantCulture),
        ["isAllDay"] = IsAllDay,
        ["organizer"] = Organizer
      };
    }

    private const string DATE_FORMAT = "yyyy-MM-ddTHH:mm:ss'Z'";

    // The request asks for UTC ("Prefer: outlook.timezone"), so 'dateTime' is UTC without an offset.
    private static DateTime parseTime(JToken Token, ILogger Logger)
    {
      string strTime = (string)Token?["dateTime"] ?? throw new InvalidOperationException("An event has no start or end time.");
      string strZone = (string)Token["timeZone"];
      if (strZone.isNotEmpty() && !string.Equals(strZone, "UTC", StringComparison.OrdinalIgnoreCase))
      {
        Logger.writeWarning($"Time zone '{strZone}' instead of UTC, the time is taken as UTC.");
      }
      return DateTime.Parse(strTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
  }
}
