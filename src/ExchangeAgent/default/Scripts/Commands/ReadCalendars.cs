/*****************************************************************************
 * Command ReadCalendars (Exchange Agent example).
 *
 * Reads the calendars of the configured meeting rooms through Microsoft Graph
 * (users/{room}/calendar/calendarView, read only) and publishes the events as
 * variables of the OPC-UA server.
 *
 * Needs two devices in the node space, found by their property "Endpoint":
 *   Endpoint = Exchange  the WebControl device "GraphClient" (commands GetCalendarView, GetNextPage)
 *   Endpoint = Calendar  the NoDevice device "RoomCalendars" (RAM only, carries the OPC-UA nodes)
 *
 * Every group of RoomCalendars with the property Role = Room is one room. Its property
 * RoomId is the object id of the room mailbox. A room without RoomId is skipped.
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
  /// Reads the room calendars from Microsoft Graph and writes the events into the OPC-UA nodes.
  /// </summary>
  public class ReadCalendars : TAbstractCommandScriptObject2
  {
    private const string ENDPOINT_GRAPH = "Exchange";
    private const string ENDPOINT_CALENDAR = "Calendar";
    private const string ROLE_ROOM = "Room";
    private const string DATE_FORMAT = "yyyy-MM-ddTHH:mm:ss'Z'";
    private const int MAX_PAGES = 100;

    // One run at a time: if Graph is slow, the next timer tick must not overtake the running one.
    private static readonly SemaphoreSlim s_Gate = new SemaphoreSlim(1, 1);

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

        //find the two devices, the same way for every command script
        INodeSpace NodeSpace = (INodeSpace)CallContext.CallingNode.Relations.FirstOrDefault(n => n is INodeSpace) ??
          throw new ArgumentException("Unable to find the node space.");
        IGroupRelation GraphDevice = (IGroupRelation)NodeSpace.queryNodeLocally(n => n.hasProperty("Endpoint", ENDPOINT_GRAPH)) ??
          throw new ArgumentException($"Unable to find the Graph device, it must have the property 'Endpoint' with value '{ENDPOINT_GRAPH}'.");
        IGroupRelation CalendarDevice = (IGroupRelation)NodeSpace.queryNodeLocally(n => n.hasProperty("Endpoint", ENDPOINT_CALENDAR)) ??
          throw new ArgumentException($"Unable to find the calendar device, it must have the property 'Endpoint' with value '{ENDPOINT_CALENDAR}'.");

        //settings on the calendar device
        int iLookAheadHours = Math.Max(1, CalendarDevice.getProperty<int>("LookAheadHours", 168));
        int iPageSize = Math.Max(1, CalendarDevice.getProperty<int>("PageSize", 50));
        bool bMailAddresses = CalendarDevice.getProperty<bool>("IncludeMailAddresses", false);

        DateTime From = DateTime.UtcNow;
        DateTime To = From.AddHours(iLookAheadHours);
        Logger.writeDebug($"Window: {From.ToString(DATE_FORMAT, CultureInfo.InvariantCulture)} to {To.ToString(DATE_FORMAT, CultureInfo.InvariantCulture)}, page size {iPageSize}.");

        List<IGroupRelation> lstRooms = CalendarDevice.queryNodesLocally(n => n is IGroupRelation && n.hasProperty("Role", ROLE_ROOM))
                                                      .Cast<IGroupRelation>()
                                                      .OrderBy(n => n.Name, StringComparer.Ordinal)
                                                      .ToList();
        if (lstRooms.Count == 0)
        {
          throw new ArgumentException($"The calendar device has no group with the property 'Role' = '{ROLE_ROOM}'.");
        }

        int iRead = 0;
        int iFailed = 0;
        int iSkipped = 0;
        foreach (IGroupRelation Room in lstRooms)
        {
          Token.ThrowIfCancellationRequested();
          string strRoomId = Room.getProperty<string>("RoomId", null);
          if (strRoomId.isEmpty() || strRoomId.StartsWith("<"))
          {
            iSkipped++;
            setValue(Room, "Status", "Not configured: the property RoomId of this group is empty");
            Logger.writeInfo($"Room '{Room.Name}': no RoomId configured, skipped.");
            continue;
          }

          try
          {
            List<TCalendarEvent> lstEvents = await readRoomAsync(GraphDevice, strRoomId, From, To, iPageSize, Token).ConfigureAwait(false);
            publishRoom(Room, lstEvents, bMailAddresses);
            iRead++;
            Logger.writeInfo($"Room '{Room.Name}': {lstEvents.Count} event(s).");
          }
          catch (Exception Exc) when (Exc.isNotCancelException())
          {
            //keep the events of the last good run in the nodes, only the status tells that they are old
            iFailed++;
            setValue(Room, "Status", $"Error: {Exc.Message}");
            Logger.writeError($"Room '{Room.Name}': reading the calendar failed. {Exc.Message}");
          }
        }

        setValue(CalendarDevice, "LastRun", DateTime.UtcNow);
        setValue(CalendarDevice, "Status", $"{iRead} room(s) read, {iFailed} failed, {iSkipped} not configured");
        Logger.writeInfo($"Completed: {iRead} room(s) read, {iFailed} failed, {iSkipped} not configured.");
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

    /// <summary>
    /// Reads all events of one room in the window, following '@odata.nextLink' until the last page.
    /// </summary>
    private async Task<List<TCalendarEvent>> readRoomAsync(IGroupRelation GraphDevice, string strRoomId, DateTime From, DateTime To, int iPageSize, CancellationToken Token)
    {
      List<TCalendarEvent> lstRetval = new List<TCalendarEvent>();

      TCommandArgs Args = new TCommandArgs();
      Args.Input["UserId"] = Uri.EscapeDataString(strRoomId);
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
          TCalendarEvent Event = TCalendarEvent.parse((JObject)Item, Logger);
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
    /// Writes the events of one room into its nodes: the summary nodes, the JSON list and the event slots.
    /// </summary>
    private void publishRoom(IGroupRelation Room, List<TCalendarEvent> lstEvents, bool bMailAddresses)
    {
      DateTime Now = DateTime.UtcNow;
      setValue(Room, "EventCount", lstEvents.Count);
      setValue(Room, "Busy", lstEvents.Any(n => n.Start <= Now && n.End > Now));
      setValue(Room, "Events", JsonConvert.SerializeObject(new JArray(lstEvents.Select(n => n.toJson(bMailAddresses))), Formatting.None));

      //the slots Event01, Event02, ... of the group take the first events, the rest is only in 'Events'
      int iSlot = 0;
      while (true)
      {
        iSlot++;
        IGroupRelation Slot = Room.queryNodeLocally(n => n.Name == $"Event{iSlot:D2}") as IGroupRelation;
        if (Slot == null)
        {
          if (iSlot <= lstEvents.Count)
          {
            Logger.writeDebug($"Room '{Room.Name}' has {lstEvents.Count} events but only {iSlot - 1} slots, see the node 'Events' for all of them.");
          }
          break;
        }
        TCalendarEvent Event = iSlot <= lstEvents.Count ? lstEvents[iSlot - 1] : null;
        setValue(Slot, "Subject", Event?.Subject ?? "");
        setValue(Slot, "Start", Event?.Start ?? s_EmptyTime);
        setValue(Slot, "End", Event?.End ?? s_EmptyTime);
        setValue(Slot, "Organizer", Event?.OrganizerName ?? "");
        setValue(Slot, "AttendeeCount", Event?.Attendees.Count ?? 0);
        setValue(Slot, "EventId", Event?.Id ?? "");
      }

      setValue(Room, "LastUpdate", DateTime.UtcNow);
      setValue(Room, "Status", "OK");
    }

    // OPC-UA has no empty date: an unused slot shows the OPC-UA zero time.
    private static readonly DateTime s_EmptyTime = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
  /// An event of a room calendar (only the fields the example publishes).
  /// </summary>
  public class TCalendarEvent
  {
    public string Id { get; private set; }
    public string Subject { get; private set; }
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public bool IsAllDay { get; private set; }
    public bool IsCancelled { get; private set; }
    public string OrganizerName { get; private set; }
    public string OrganizerMail { get; private set; }

    /// <summary>Persons only: rooms and equipment that are invited to the event are not attendees.</summary>
    public List<TCalendarAttendee> Attendees { get; private set; } = new List<TCalendarAttendee>();

    /// <summary>
    /// Reads one element of the Graph answer. Missing fields stay empty, a missing start or end is an error.
    /// </summary>
    public static TCalendarEvent parse(JObject Item, ILogger Logger)
    {
      TCalendarEvent Retval = new TCalendarEvent
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

      if (Item["attendees"] is JArray Attendees)
      {
        foreach (JToken Attendee in Attendees)
        {
          if ((string)Attendee["type"] == "resource")
          {
            continue;
          }
          Retval.Attendees.Add(new TCalendarAttendee
          {
            Name = (string)Attendee.SelectToken("emailAddress.name") ?? "",
            Mail = (string)Attendee.SelectToken("emailAddress.address") ?? "",
            Response = (string)Attendee.SelectToken("status.response") ?? ""
          });
        }
      }
      return Retval;
    }

    /// <summary>
    /// The JSON of one event as published in the node 'Events'. Mail addresses only on request.
    /// </summary>
    public JObject toJson(bool bMailAddresses)
    {
      JObject Organizer = new JObject { ["name"] = OrganizerName };
      if (bMailAddresses)
      {
        Organizer["mail"] = OrganizerMail;
      }
      JArray Persons = new JArray();
      foreach (TCalendarAttendee Attendee in Attendees)
      {
        JObject Person = new JObject { ["name"] = Attendee.Name, ["response"] = Attendee.Response };
        if (bMailAddresses)
        {
          Person["mail"] = Attendee.Mail;
        }
        Persons.Add(Person);
      }
      return new JObject
      {
        ["id"] = Id,
        ["subject"] = Subject,
        ["start"] = Start.ToString(DATE_FORMAT, CultureInfo.InvariantCulture),
        ["end"] = End.ToString(DATE_FORMAT, CultureInfo.InvariantCulture),
        ["isAllDay"] = IsAllDay,
        ["organizer"] = Organizer,
        ["attendees"] = Persons
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

  /// <summary>
  /// A person invited to an event.
  /// </summary>
  public class TCalendarAttendee
  {
    public string Name { get; set; }
    public string Mail { get; set; }
    public string Response { get; set; }
  }
}
