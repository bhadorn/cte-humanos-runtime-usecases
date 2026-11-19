---
id: bookable-machine
title: "Use Case: Bookable Machine (Outlook booking and FANUC state to OPC-UA)"
subject: "Booking a machine in an Outlook calendar and comparing the booking with the running state of a FANUC controller, published as OPC-UA variables"
keywords: [HumanOS, Exchange, Outlook, equipment mailbox, Microsoft Graph, Entra, calendarView, booking, FANUC, FOCAS2, NCGuide, WebControl, FanucControl, NoDevice, OPC-UA, timer, rule]
---

# Bookable Machine (Outlook booking and FANUC state to OPC-UA)

Shows how a machine becomes something people **book in Outlook**, and how the gateway **checks the booking against the machine**. The machine has an Exchange *equipment mailbox* (a resource calendar). The gateway reads that calendar through Microsoft Graph (read only), reads the running state of a FANUC controller, and publishes both together with a verdict: is the machine used as booked?

It is built from the [Exchange Agent](../ExchangeAgent/Readme.md) example (same Graph device, same calendar script, same local timer) and adds a FANUC device and the comparison. Read that Readme for everything about the Entra app, the access policy, the secret and the OPC-UA client; this Readme repeats only what is different.

## Architecture

```text
 Outlook: people book the machine            FANUC controller (or NCGuide simulator)
 (invite the equipment mailbox)                        │  FOCAS2, polled by the FanucControl driver
        │                                              ▼
        ▼                                     Device Machine (FanucControl)
 Microsoft Graph  ◀── HTTPS, Entra app ──┐      Controller/RunningState ...
 users/{id}/calendar/calendarView        │              │
                                         │              │
                                ┌────────┴──────────────▼──────────────────────┐
                                │ HumanOS gateway                              │
                                │  timer (15 s) ─▶ command ReadBooking         │
                                │    1. reads the calendar (GraphClient)       │
                                │    2. reads Machine/Controller/RunningState  │
                                │    3. writes Usage and Mismatch              │
                                │ Device MachineBooking (NoDevice, RAM)        │
                                └───────────────────────┬──────────────────────┘
                                                        ▼
                                           OPC-UA server (port 4840) ──▶ UaExpert, SCADA, MES
```

### What the example answers

| Usage                   | Booked now | Machine runs | Meaning                                                  | `Mismatch` |
| :---------------------- | :--------- | :----------- | :------------------------------------------------------- | :--------- |
| `Free`                  | no         | no           | nobody uses the machine                                  | false      |
| `BookedIdle`            | yes        | no           | booked, but the program does not run (set-up, break)     | false      |
| `InUse`                 | yes        | yes          | used as booked                                           | false      |
| `RunningWithoutBooking` | no         | yes          | **the machine runs but nobody has booked it**            | **true**   |
| `MachineUnknown`        | -          | unknown      | the controller is not readable (not connected)           | false      |
| `BookingUnknown`        | unknown    | -            | the calendar could not be read (see `Status`)            | false      |

"Runs" means the FANUC running state is `START` (3: a program is running). `HOLD`, `STOP` and `RESET` count as not running. `BookedIdle` is **not** a mismatch on purpose: a booked machine is often idle. Change the rule in `ReadBooking.cs` (`publishUsage`) if you want something else, for example a warning when a booking has started 30 minutes ago and the machine never ran.

## Prerequisites

- Everything the [Exchange Agent](../ExchangeAgent/Readme.md#prerequisites) needs, with an **equipment mailbox** (or a room mailbox) for the machine instead of a meeting room. Without a Microsoft 365 tenant you can use the mock server of the Exchange Agent (see [Try it without Microsoft 365](#try-it-without-microsoft-365-mock-server)).
- The **FANUC NCGuide** simulator (the project is pre-configured for it) **or** a physical FANUC control with FOCAS2 over Ethernet (0i-D/F; the FANUC option *OpenCNC* must be enabled for Ethernet). The FANUC library `Fwlib32.dll` is part of the runtime installation (it was found in the folder of the trial runtime 2.12); it belongs to FANUC and is **not** part of this repository.
- HumanOS IoT Designer 2.12 (includes the trial runtime), an OPC-UA client such as UaExpert.

## Project contents

| File                                                           | Purpose                                                                                           |
| :------------------------------------------------------------- | :------------------------------------------------------------------------------------------------ |
| `BookableMachine.h2proj`                                       | Designer project, target `default` (trial runtime 2.12)                                          |
| `default/Devices/GraphClient.json`                             | Microsoft Graph client (WebControl), same as in the Exchange Agent                                |
| `default/Devices/MachineBooking.json`                          | NoDevice (RAM) device with the booking and usage nodes. **Here you enter the mailbox id** (`ResourceId`) |
| `default/Devices/Machine.json`                                 | FANUC device `Machine`: address and the nodes `Controller/RunningState`, `AlarmState`, `OperationMode`, `MainProgram`, `Available` |
| `default/Scripts/Commands/ReadBooking.cs`                      | Reads the calendar and the machine state, writes the nodes                                        |
| `default/Scripts/Rules/ReadBookingTimer.cs`                    | Rule script: the timer starts `ReadBooking`                                                       |
| `default/DataModel/Objects/BookingPolling.json`                | Object with the command, the timer (every 15 s, first run after 10 s) and the rule               |
| `default/HumanOS.UHAL.WebControl/GetCalendarView.cs`, `GetNextPage.cs` | Graph requests (unchanged copies from the Exchange Agent)                                  |
| `default/HumanOS.UHAL.FanucControl/settings.json`              | Driver settings (copy from the example *FANUC Data Aggregation*)                                  |
| `Setup/New-EquipmentMailbox.ps1`                               | Creates the equipment mailbox and prints its object id                                            |

The Entra app and the access policy use the scripts of the Exchange Agent: `..\ExchangeAgent\Setup\New-GraphApp.ps1` and `..\ExchangeAgent\Setup\Set-AccessPolicy.ps1`.

**No secret and no tenant data is stored in the project.** The Entra values are placeholders (`<TENANT-ID>`, `<CLIENT-ID>`, `<MAILBOX-OBJECT-ID>`), the client secret lives in the secret database of **your** copy of the project. The project file contains the same **throw-away vault key** as every other example of this repository: set your own key before you enter a secret, and never commit a project file with a filled secret database (see [Exchange Agent, step 5b](../ExchangeAgent/Readme.md#step-5-configure-the-designer-project)). Do not commit the address of a real control or other data of your own network.

## Step 1: Register the Entra app

Exactly as in [Exchange Agent, step 1](../ExchangeAgent/Readme.md#step-1-register-the-entra-app): app registration with the application permission `Calendars.ReadBasic.All`, admin consent, a client secret. You need `<TENANT-ID>`, `<CLIENT-ID>` and the secret value. If you already ran that example you can reuse the app.

## Step 2: Create the equipment mailbox

An equipment mailbox is an Exchange resource with its own calendar. People book it by inviting it as a resource in Outlook. **Do not use a mailbox that people really book** for the first tests.

```powershell
.\Setup\New-EquipmentMailbox.ps1 -AdminUpn <ADMIN-UPN> -MachineMail demo-machine@<DOMAIN> -MachineName "FANUC Demo"
```

The script creates the mailbox, accepts bookings automatically when the machine is free (`AutomateProcessing = AutoAccept`) and keeps the subject and the organizer visible (`DeleteSubject` and `AddOrganizerToSubject` are `$false`; otherwise bookings without a usable subject are published). It prints the **object id of the mailbox in Entra**: that is `<MAILBOX-OBJECT-ID>`. A room mailbox works the same way (`-Room`); the code does not care whether the resource is equipment or a room.

You can do the same by hand in the Exchange admin center (*Recipients, Resources, Add equipment*) and read the id with `(Get-Mailbox -Identity demo-machine@<DOMAIN>).ExternalDirectoryObjectId`. It is **not** the id of a place from Graph's places list.

## Step 3: Restrict the access to the machine mailbox (recommended)

`Calendars.ReadBasic.All` reaches every mailbox of the directory until an application access policy limits the app to a group of mailboxes. Run the script of the Exchange Agent with the machine mailbox in place of the room (see [its step 4](../ExchangeAgent/Readme.md#step-4-restrict-the-access-to-the-rooms-recommended) for the details and the waiting time of up to an hour):

```powershell
..\ExchangeAgent\Setup\Set-AccessPolicy.ps1 -AdminUpn <ADMIN-UPN> -AppId <CLIENT-ID> `
  -RoomMail demo-machine@<DOMAIN> -GroupMail demo-machine-access@<DOMAIN> -GroupName "Bookable Machine"
```

## Step 4: Configure the Designer project

1. **Tenant id, client id, client secret** as in [Exchange Agent, step 5a and 5b](../ExchangeAgent/Readme.md#step-5-configure-the-designer-project): the variables `GRAPH_TENANT_ID` and `GRAPH_CLIENT_ID` of the device `GraphClient` (in `BookableMachine.h2proj`), the secret `GraphClientSecret` in the secret database after you set your own vault key.
2. **Mailbox id:** in `default/Devices/MachineBooking.json` set the property `ResourceId` to `<MAILBOX-OBJECT-ID>`. Without it the calendar is not read: `Status` says *Not configured* and `Usage` is `BookingUnknown` (see [Troubleshooting](#troubleshooting)).
3. **Machine:** the device `Machine` in `default/Devices/Machine.json` points to the NCGuide simulator (`focas2://hssb@NCGUIDE`, `Type` `FANUC 0i-F`). For a real control see [A real FANUC control](#a-real-fanuc-control).

The Designer builds from the device files of the target (`default/Devices/...`), not from the templates in `DeviceTemplates/`. Change a template as well if you create new devices from it.

## Step 5: Build and start the gateway

Build the target `default` in the Designer (output `Build\default\`), start the trial runtime with `"-AppSettingsDir=<project folder>\Build\default"`, exactly as in [Exchange Agent, step 6](../ExchangeAgent/Readme.md#step-6-build-and-start-the-gateway). The trial runtime runs for one hour.

Ten seconds after the start the timer fires for the first time, then every 15 seconds:

```text
ReadBooking: Started.
ReadBooking: Calendar: 1 booking(s).
ReadBooking: Usage: BookedIdle.
ReadBooking: Completed.
```

The first run after a start can show `MachineUnknown` while the driver connects. Graph is asked every 15 seconds (four requests per minute for one mailbox, far below the limits of Graph). A longer interval makes the verdict slower: `Mismatch` is evaluated at the same rhythm.

## Step 6: Look at the result with an OPC-UA client

**First book the machine**: in Outlook create an appointment of 30 minutes that **starts a few minutes ago** and invite the machine mailbox as a resource. Connect UaExpert to `opc.tcp://localhost:4840` as described in [Exchange Agent, step 7](../ExchangeAgent/Readme.md#step-7-look-at-the-result-with-an-opc-ua-client) (the same security notice applies: anonymous login and security mode `None`, use a test network only).

The server publishes two devices of this project (`GraphClient` is published, too, as in the Exchange Agent, and holds only commands):

| Device (variable)                          | Type     | Meaning                                                              |
| :----------------------------------------- | :------- | :------------------------------------------------------------------- |
| `MachineBooking/Booked`                    | Boolean  | a booking is running now                                             |
| `MachineBooking/BookedBy`                  | String   | organizer of the running booking, empty if none                      |
| `MachineBooking/BookedSubject`             | String   | subject of the running booking                                       |
| `MachineBooking/BookedUntil`               | DateTime | end of the running booking (UTC), `1601-01-01` if none               |
| `MachineBooking/NextStart`, `NextBookedBy` | DateTime, String | start and organizer of the next booking                      |
| `MachineBooking/EventCount`, `Events`      | Int32, String | number of bookings in the look-ahead window and all of them as JSON |
| `MachineBooking/MachineRunning`            | Boolean  | the FANUC running state is `START`                                   |
| `MachineBooking/Usage`, `Mismatch`         | String, Boolean | the verdict, see [the table above](#what-the-example-answers)  |
| `MachineBooking/Status`, `LastRun`, `LastUpdate` | String, DateTime | `OK` or the error of the last run, when it ran and when the calendar was read last |
| `Machine/Controller/RunningState` ...      | Int32, String | live values of the controller (`Available`, `AlarmState`, `OperationMode`, `MainProgram`) |

All variables are read only for OPC-UA clients. `LookAheadHours` (default 48) and `PageSize` (default 50) are properties of the device `MachineBooking`.

## Try it without Microsoft 365 (mock server)

The mock server of the Exchange Agent answers the one request the example sends (`python ..\ExchangeAgent\MockGraph\mock_graph.py`, listening on `http://127.0.0.1:8099/v1.0`). Change two things in the device files and **build the target again**:

1. `default/Devices/GraphClient.json`: `Address` to `http://127.0.0.1:8099/v1.0` and the property `OAuthEnable` to `false`. Change the `Address` of the device entry `GraphClient` in `BookableMachine.h2proj` to the same value.
2. `default/Devices/MachineBooking.json`: `ResourceId` to one of the mock ids:

| `ResourceId`                           | Answer of the mock                                            | `Usage` with a stopped machine | with a running machine   |
| :------------------------------------- | :------------------------------------------------------------ | :----------------------------- | :----------------------- |
| `11111111-1111-1111-1111-111111111111` | 12 bookings, one running now                                  | `BookedIdle`                   | `InUse`                  |
| `33333333-3333-3333-3333-333333333333` | an empty calendar                                             | `Free`                         | `RunningWithoutBooking`  |
| `44444444-4444-4444-4444-444444444444` | HTTP 403 `ErrorAccessDenied`                                  | `BookingUnknown`               | `BookingUnknown`         |

The mock has no authentication: use it on `localhost` only.

## A real FANUC control

The device `Machine` is pre-configured for the NCGuide simulator. For a real control change in `default/Devices/Machine.json` and in the device entry of `BookableMachine.h2proj`:

- **`Address`**: `focas2://<CONTROL-HOST>:8193` (FOCAS2 over Ethernet; `<CONTROL-HOST>` is the IP address or host name of the control). Do not commit your own address.
- **`Type`** (property): `FANUC 0i-D` or `FANUC 0i-F`, as your control is.
- **Secret**, if the control asks for authentication: a secret in the secret database and the property `SecretName` of the device (see the [FANUC driver reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FanucControl/)). A wrong secret shows as `Authentication error ... EW_AUTH` in the log.
- The addresses of the nodes (`Nc1.Dynamic.Float64:5` for the running state) are the standard ones of the driver and need no change for 0i-D/F.

NCGuide: the address of the simulator changed in newer runtimes. `focas2://hssb@NCGUIDE` (and the short form `NCGUIDE`) is valid, the old `NCGUIDE!` is **refused** (`Invalid address 'focas2://hssb@NCGUIDE!'`). NCGuide itself must be installed and running, otherwise the driver logs `NCGUIDE not running or driver DLL not available ... EW_MMCSYS` and the usage is `MachineUnknown`.

## How it works

- **One command, one timer.** `BookingPolling.json` holds the command `ReadBooking`, a timer and a rule that belong together (a rule only sees nodes of its own group). The rule script `ReadBookingTimer.cs` looks up the command by name and calls it. The command lets only one run at a time. The object has a different name from the command on purpose: a lookup by name would find the object first.
- **Devices are found by the property `Endpoint`:** `Exchange` (GraphClient), `Calendar` (MachineBooking) and `Machine` (the FANUC device). The command needs the first two; **without** a `Machine` device it still runs and says `MachineUnknown`.
- **The machine state is read from the node, not from the control.** The command asks the node `Machine/Controller/RunningState` for the value that the FANUC driver delivered last. If the driver is not connected, the value is *not good* and the usage is `MachineUnknown`. So the command never blocks on the control.
- **Calendar reading** is the code of the Exchange Agent for one mailbox: `calendarView` of the next `LookAheadHours`, `@odata.nextLink` paging, cancelled events dropped, times in UTC. The booking that has started and not yet ended is the current one.
- **A failing calendar keeps the old booking nodes**; `Status` shows the error and the verdict becomes `BookingUnknown` (never a false `Mismatch`).

## Limits and security notes

- **Read only.** The app can read calendars, it cannot book. Whoever books, books in Outlook (or with any other tool of your organization).
- **Showing the booker.** `BookedBy` publishes the name of the organizer to everybody who can read the OPC-UA server. The mail address is only published with the device property `IncludeMailAddresses`. Keep the OPC-UA server in a trusted network (anonymous login, security mode `None`).
- **Free/busy flags are ignored.** An event that Outlook shows as *free* or *tentative* counts as a booking, an all-day event books the whole day.
- **The booking and the machine are two views.** The example finds a mismatch, it does not stop a machine. Do not use it for safety functions.
- **`Calendars.ReadBasic.All` reaches all mailboxes** until you restrict it (step 3).

## Test status

What was run, and what was not (state of 9 November 2026):

- **Run:** the scripts were compiled by the runtime 2.12 (trial) without errors. The target was assembled by hand (not built with the Designer) from the files of this project and started with the mock server of the Exchange Agent. All six verdicts were produced: `InUse`, `BookedIdle`, `RunningWithoutBooking` and `Free` with the running state of a test device (a RAM device instead of the FANUC driver, toggled by a test rule), `BookingUnknown` with the mock's HTTP 403, and `MachineUnknown` with the real FANUC driver and no NCGuide running (`BadNotActive`, log message `NCGUIDE not running or driver DLL not available`). The OPC-UA server added the devices `MachineBooking` and `Machine`.
- **Not run:** the project file `BookableMachine.h2proj` was written by script and **not opened in the Designer**. A real Exchange equipment mailbox and the Entra login were **not** tried with this project (the Graph code is the one of the Exchange Agent, which was tried against Outlook). `New-EquipmentMailbox.ps1` was **not** run. NCGuide and a real FANUC control were **not** connected, so the values of `RunningState` from a real control and the mapping `3 = START` rely on the driver manual. The OPC-UA variables were not looked at with an OPC-UA client.

## Troubleshooting

| Symptom                                                        | Cause and remedy                                                                                              |
| :------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------ |
| `Usage` is `MachineUnknown`                                    | The controller is not connected (log of `FanucControlDriver`): NCGuide or the control not running or not reachable, wrong address. A first run right after the start can show it once. |
| `Invalid address 'focas2://hssb@NCGUIDE!'`                     | Old address notation. Use `focas2://hssb@NCGUIDE`.                                                            |
| `Usage` is `BookingUnknown`, `Status` says `Error: ...`        | The calendar could not be read: HTTP 403 (access policy or consent), 404 (wrong `ResourceId`), `AADSTS...` (login). See the troubleshooting of the [Exchange Agent](../ExchangeAgent/Readme.md#troubleshooting). |
| `Status` says *Not configured*                                 | `ResourceId` of `MachineBooking` is empty or still `<MAILBOX-OBJECT-ID>`. The calendar is not read, the verdict is `BookingUnknown`. |
| `Booked` is false although a booking exists                    | The booking is not running now, or the mailbox did not accept it (check `AutomateProcessing`), or the subject is empty because `DeleteSubject` is `$true`. |
| `Unable to find the device with the property 'Endpoint' = 'Machine'` | The FANUC driver did not load the device (see above). The command still runs.                          |

## Documentation

- [Exchange Agent example](../ExchangeAgent/Readme.md): Entra app, equipment and room mailboxes, access policy, secret, OPC-UA client, Graph details
- [FANUC Control driver reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FanucControl/): addresses, secrets, data access (`Nc1.Dynamic.Float64:5` running state)
- [FANUC Data Aggregation example](../FANUC.DataAggregation/Readme.md): the FANUC device on its own, without the booking
- [OPC-UA Server reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
