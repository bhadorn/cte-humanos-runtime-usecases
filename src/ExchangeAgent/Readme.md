---
id: usecase-exchange-agent
title: "Use Case: Exchange Agent (Outlook room calendars to OPC-UA)"
subject: "Reading the calendars of Outlook meeting rooms through Microsoft Graph (read only) and publishing the events as OPC-UA variables"
keywords: [HumanOS, Exchange, Outlook, Microsoft Graph, Entra, calendarView, room calendar, WebControl, OAuth, client credentials, OPC-UA, NoDevice, timer, rule]
---

# Exchange Agent (Outlook room calendars to OPC-UA)

Shows how a HumanOS gateway reads the **calendars of Outlook meeting rooms** from Microsoft 365 and makes the appointments visible to any OPC-UA client, for example on a door display, in a SCADA system or in a MES. The gateway logs in to Microsoft Graph as an **application** (Entra app, client credentials, **read only**), asks for the next days of every configured room (`users/{room}/calendar/calendarView`, following `@odata.nextLink` page by page) and writes the events into variables of the OPC-UA server. The calendars are polled once a minute.

The project reads only. Creating, changing or deleting appointments is **not** part of this example (see [Limits](#limits-and-security-notes)).

## Architecture

```text
Microsoft 365                                   HumanOS gateway (Runtime 2.12)                       OPC-UA clients
┌─────────────────────┐   HTTPS, OAuth          ┌──────────────────────────────────────────┐
│ Entra app           │◀─ client credentials ───│ Device GraphClient (WebControl)          │
│  Calendars.         │                         │   GetCalendarView, GetNextPage           │
│  ReadBasic.All      │   calendarView (+paging)│            ▲                             │
│ Room mailboxes      │◀────────────────────────│            │ called by                   │
│  (Exchange Online)  │                         │ Command ReadCalendars (C# script)        │
└─────────────────────┘                         │            ▲                             │
                                                │            │ every 60 s                  │
                                                │ Timer + rule "RunReadCalendars"          │
                                                │            │ writes                      │
                                                │            ▼                             │   opc.tcp://<host>:4840/
                                                │ Device RoomCalendars (NoDevice, RAM)     │───────────────────────────▶ UaExpert, SCADA, ...
                                                │   Room1, Room2 ... ─ Status, Busy, Events│
                                                │   Event01 ... Event10 ─ Subject, Start...│
                                                │ OPC-UA server plugin                     │
                                                └──────────────────────────────────────────┘
```

### Terms used in this document

| Term                          | Meaning                                                                                                                              |
| :---------------------------- | :----------------------------------------------------------------------------------------------------------------------------------- |
| **Designer**                  | The HumanOS IoT Designer, the tool in which you edit a project (`*.h2proj`) and build it.                                            |
| **Target**                    | A build variant of the project (here `default`). The build writes the runnable configuration to `Build\<target>\`.                    |
| **Gateway**                   | `HumanOS.IoT.Gateway.exe` of the runtime: the program that runs the configuration of the target.                                     |
| **Device**                    | A data source or sink of the gateway, driven by a **driver**. Here: `GraphClient` (driver *WebControl*, an HTTP client with OAuth) and `RoomCalendars` (driver *NoDevice*, values only in memory). |
| **Command**                   | An action of a device or a DataModel object, started by a script, a rule or an OPC-UA client (as an OPC-UA *method*).                |
| **DataModel object**          | A JSON file (`default/DataModel/Objects`) with nodes of the gateway's own model: here the command, a timer and a rule.                |
| **Rule**                      | Reacts to an event (here: the timer tick) and runs an action (here: the script that starts the command).                              |
| **Secret database**           | Encrypted list of passwords in the project. At the build the entries are copied to `Build\<target>\Secrets\`.                         |
| **Node space**                | The gateway's in-memory tree of all devices, groups and values. The OPC-UA server publishes part of it.                               |
| **Platform / Agent**          | The HumanOS Platform is a separate central system; an *agent* is a gateway project that works for it. This example needs neither.     |
| **MSAL**                      | Microsoft's login library. The gateway uses it for the OAuth exchange with Entra.                                                     |
| **Throttling**                | Graph refuses an app that asks too often (HTTP 429). One request per room and minute is far below the limits.                         |

## Prerequisites

- **HumanOS IoT Designer** with the HumanOS IoT Runtime **2.12** (checked with the trial runtime 2.12.0.6882, x86). The trial runtime is enough; it stops one hour after the start.
- **Runtime 2.11 or newer** for the command script: since 2.11 a command script derives from `TAbstractCommandScriptObject2` (`executeCommandAsync(ICommandCallContext CallContext, CancellationToken Token)`). An older command script fails at the first call with *Could not find object type 'TAbstractCommandScriptObject2'*; a script written for the old base class has to be ported.
- A **Microsoft 365 directory (Entra tenant) with Exchange Online** and administrators who may register an app, grant admin consent and create a room mailbox. Without a tenant you can still try the gateway with the [mock server](#try-it-without-microsoft-365-mock-server).
- An **OPC-UA client** to look at the result, for example [UaExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html).
- **PowerShell 5.1 or newer** with the modules `Microsoft.Graph.Applications` (step 1) and `ExchangeOnlineManagement` (steps 2 to 4). You need them for the scripts in `Setup\` and for the Exchange commands of steps 3 and 4; only the Entra part of step 1 and the room of step 2 can also be done in the portals. `Install-Module` may ask you to trust the PowerShell Gallery and to install the NuGet provider: answer yes. The first sign-in of `New-GraphApp.ps1` asks for the consent of the *Microsoft Graph Command Line Tools* (permissions `Application.ReadWrite.All`, `AppRoleAssignment.ReadWrite.All`): an Entra administrator has to accept it.
- Run the scripts from the folder `src\ExchangeAgent` (they are called as `.\Setup\<name>.ps1`). If PowerShell refuses to run them (execution policy, or *Mark of the Web* after a ZIP download), run `Unblock-File .\Setup\*.ps1` and, if needed, `Set-ExecutionPolicy -Scope Process RemoteSigned` in that window.
- Optional: Python 3.8 or newer for the mock server.

## Project Contents

| Path                                                        | Purpose                                                                                                |
| :---------------------------------------------------------- | :----------------------------------------------------------------------------------------------------- |
| `ExchangeAgent.h2proj`                                      | Designer project: target `default`, the two devices, the device variables, the secret database          |
| `DeviceTemplates/MicrosoftGraph.json`                       | Template of the Graph client (WebControl, OAuth settings, commands `GetCalendarView` and `GetNextPage`) |
| `DeviceTemplates/RoomCalendars.json`                        | Template of the device that carries the OPC-UA nodes (NoDevice driver)                                  |
| `default/Devices/GraphClient.json`                          | Device `GraphClient` of the target                                                                      |
| `default/Devices/RoomCalendars.json`                        | Device `RoomCalendars` of the target: two rooms, ten event slots each. **Here you enter the room ids**  |
| `default/DataModel/Objects/CalendarPolling.json`            | Command `ReadCalendars`, the timer (10 s after the start, then every 60 s) and the rule that starts the command |
| `default/Scripts/Commands/ReadCalendars.cs`                 | The command: reads the calendars, follows the paging, writes the nodes                                  |
| `default/Scripts/Rules/ReadCalendarsTimer.cs`               | Rule script: starts the command on every timer tick                                                     |
| `default/HumanOS.UHAL.WebControl/GetCalendarView.cs`        | HTTP script of the command `GetCalendarView` (requests UTC times, reads Graph errors)                   |
| `default/HumanOS.UHAL.WebControl/GetNextPage.cs`            | HTTP script of the command `GetNextPage` (calls the `@odata.nextLink` URL)                              |
| `default/HumanOS.PeSeL.OPCUAServer/settings.json`           | OPC-UA server: port 4840, anonymous login, only the device `RoomCalendars` is published                 |
| `default/HumanOS.UHAL.NoDevice/settings.json`, `.../WebControl/settings.json`, `.../DeviceDetectors/settings.json` | Plugin settings                                                  |
| `Setup/New-GraphApp.ps1`, `New-RoomMailbox.ps1`, `Set-AccessPolicy.ps1` | Entra app, room mailbox and access policy from the PowerShell (steps 1 to 4)                |
| `Setup/Add-Room.ps1`                                        | Adds another room (copies a room group with new node ids)                                               |
| `MockGraph/mock_graph.py`                                   | Stand-in for the calendarView endpoint, to try the project without Microsoft 365                        |

**No secret and no tenant data is stored in the project.** The Entra values are placeholders (`<TENANT-ID>`, `<CLIENT-ID>`, `<ROOM-OBJECT-ID>`), the client secret lives in the secret database of **your** copy of the project. The project file does contain a **throw-away project vault key** (`ProjectVaultKey`, the same kind of key as in every other example of this repository). It is public: before you enter your client secret, set your own key (step 5b). Do not commit your own values to a public repository.

**Waiting times.** A new app, an admin consent, a group or a room mailbox can need a few minutes before every Microsoft service sees it (the access policy up to an hour, step 4). If a test right after the creation fails with `AADSTS700016`, HTTP 403 or 404, wait a few minutes and try again before you look for another cause.

## Step 1: Register the Entra app

The gateway logs in as an application, not as a person. Needed: **one application permission, `Calendars.ReadBasic.All`**, with admin consent, and a client secret.

`Calendars.ReadBasic.All` is enough for this example. In the test against the real test room the answer held subject, start, end, organizer and attendees; the permission leaves out body and attachments. `Calendars.Read` would give more than the example uses; `Calendars.ReadWrite` is only needed for writing and is deliberately not used. The permission applies to **all mailboxes** of the directory until you restrict it in [step 4](#step-4-restrict-the-access-to-the-rooms-recommended).

**With the script** (administrator with the Entra roles *Application Administrator* and *Privileged Role Administrator*, or *Global Administrator*):

```powershell
Install-Module Microsoft.Graph.Applications -Scope CurrentUser
.\Setup\New-GraphApp.ps1 -TenantId <TENANT-ID> -SecretValidityDays 90
```

The script creates the app registration `HumanOS Exchange Agent`, grants the admin consent and prints the **client id**, the **tenant id** and the **client secret** once. Copy the secret now, it is not shown again. `-Remove` deletes the app (and with it the secret) again.

**In the portal** ([entra.microsoft.com](https://entra.microsoft.com)):

1. *Entra ID, App registrations, New registration*: a name, *Accounts in this organizational directory only*, no redirect URI, *Register*. Copy *Application (client) ID* and *Directory (tenant) ID* from the overview page.
2. *API permissions, Add a permission, Microsoft Graph, Application permissions*, search for `Calendars.ReadBasic.All`, *Add permissions*. Then *Grant admin consent for <your directory>*. The status column must show *Granted*.
3. *Certificates & secrets, New client secret*: a description and a **short expiry** (90 days for a test, at most 24 months). Copy the **Value** (not the *Secret ID*) before you leave the page.

The secret expires. Put the expiry date in your calendar: when it has passed, the gateway can no longer log in (see [Troubleshooting](#troubleshooting)). For production consider a certificate instead of a secret; the device property `OAuthSecretSourceType` then is `Store` or `File` (see the WebControl manual). This example was only tried with the secret.

## Step 2: Create a room mailbox

A room is an Exchange resource mailbox with its own calendar. The people book it by inviting the room as a resource in Outlook. If you already have a test room you can use it; **do not use a room that people really book** for your first tests.

**With the script** (Exchange administrator):

```powershell
Install-Module ExchangeOnlineManagement -Scope CurrentUser
.\Setup\New-RoomMailbox.ps1 -AdminUpn <ADMIN-UPN> -RoomMail example-room@<DOMAIN> -RoomName "Example Room"
```

Or in the *Exchange admin center* (*Recipients, Resources, Add room*) and then in PowerShell:

```powershell
Connect-ExchangeOnline -UserPrincipalName <ADMIN-UPN>
# keep the subject and the organizer visible in the room calendar
Set-CalendarProcessing -Identity example-room@<DOMAIN> -DeleteSubject $false -AddOrganizerToSubject $false
Get-CalendarProcessing -Identity example-room@<DOMAIN> | Format-List DeleteSubject, AddOrganizerToSubject, AutomateProcessing
```

The setting matters: a room that deletes the subject (`DeleteSubject` is `$true`) shows events without a usable subject, the agent then publishes empty subjects.

## Step 3: Find the object id of the room

The agent addresses the room calendar as `users/<id>/calendar/calendarView`. The id is the **object id of the room mailbox in Entra**, a GUID. It is **not** the id of the place that `places/microsoft.graph.room` lists.

```powershell
Connect-ExchangeOnline -UserPrincipalName <ADMIN-UPN>
(Get-Mailbox -Identity example-room@<DOMAIN>).ExternalDirectoryObjectId
```

`New-RoomMailbox.ps1` prints the same value. Write it down as `<ROOM-OBJECT-ID>`.

Graph also accepts the **mail address** of the room instead of the id (`users/example-room@<DOMAIN>/calendar/...`). That was tried once against the real test room and worked. The object id is the stable choice (it does not change when the address changes), so the rest of this document uses it.

## Step 4: Restrict the access to the rooms (recommended)

`Calendars.ReadBasic.All` allows the app to read the calendar of **every** mailbox in the directory, not only of your rooms. An **application access policy** of Exchange Online limits the app to the members of a mail-enabled security group. Do this before the first run, certainly in a directory where people work.

```powershell
Connect-ExchangeOnline -UserPrincipalName <ADMIN-UPN>

# 1. mail-enabled security group that contains only the rooms the app may read
New-DistributionGroup -Name "Exchange Agent Rooms" -Type Security `
  -PrimarySmtpAddress example-room-access@<DOMAIN> -Members example-room@<DOMAIN>

# 2. the policy: this app (client id) may only reach the members of that group
New-ApplicationAccessPolicy -AppId <CLIENT-ID> -PolicyScopeGroupId example-room-access@<DOMAIN> `
  -AccessRight RestrictAccess -Description "HumanOS Exchange Agent example"

# 3. check: the room must be Granted, any other mailbox must be Denied
Test-ApplicationAccessPolicy -Identity example-room@<DOMAIN> -AppId <CLIENT-ID>
Test-ApplicationAccessPolicy -Identity <ANOTHER-MAILBOX>@<DOMAIN> -AppId <CLIENT-ID>
```

`Setup\Set-AccessPolicy.ps1` does the three steps; with `-Remove` it deletes the policy and the group again:

```powershell
.\Setup\Set-AccessPolicy.ps1 -AdminUpn <ADMIN-UPN> -AppId <CLIENT-ID> `
  -RoomMail example-room@<DOMAIN> -GroupMail example-room-access@<DOMAIN> `
  -OtherMailbox <ANOTHER-MAILBOX>@<DOMAIN>
```

`-OtherMailbox` is optional: the script then also checks that this mailbox is *Denied*. The script asks before it changes anything (`-Force` skips the question).

**Limits of the policy**

- **Role:** only an Exchange administrator (role *Organization Management*) can create it. The Entra roles of step 1 are not enough.
- **Effect:** the policy is **not effective at once**. Microsoft names a delay of up to about one hour. During that time the app still reaches mailboxes outside the group. Start the agent when `Test-ApplicationAccessPolicy` shows `Denied` for a mailbox outside the group.
- **Scope:** the policy restricts what the app can read **in Exchange Online mailboxes** (calendars, mail, contacts). It does not restrict other Graph permissions of the app, for example directory permissions such as `User.Read.All`. This example only holds `Calendars.ReadBasic.All`, so there is nothing else to restrict, but do not add permissions later and assume the policy covers them.
- **Only the app:** the policy applies to the app (its client id), not to a person. Your own sign-in (Outlook, PowerShell, the admin center) is not changed by it, and `Test-ApplicationAccessPolicy` answers for the app only. A policy therefore says nothing about your own mailbox or the mailbox of the administrator who created it.
- **Group:** the group has to be a mail-enabled security group (not a plain Entra security group). Put only the rooms into it that the app may read.
- **Successor:** Microsoft replaces application access policies step by step with *RBAC for Applications* in Exchange Online. Ask the administrator of your directory which one applies. Only the policy is described here; RBAC for Applications was not tried.

## Step 5: Configure the Designer project

You need the results of steps 1 and 3: `<TENANT-ID>`, `<CLIENT-ID>`, the client secret and `<ROOM-OBJECT-ID>`.

> **What is verified here.** The three values below were put into the project files and the build output was made by hand (see [Test status](#test-status)); the author did **not** click through the Designer for this example. The names of the Designer menus and dialogs in steps 5 and 6 were not checked against the GUI and may differ slightly in your version; search the Designer for the term if one is missing. Everything that is stored in a file (names, values, paths) is exact.

**a) Tenant id and client id.** They are *variables of the device `GraphClient`* (Designer: device `GraphClient`, variables), stored in `ExchangeAgent.h2proj` in the device entry. **Which file holds what:** the variables exist only in the project file `ExchangeAgent.h2proj`; the other settings of the device (properties, commands, OAuth settings) live in `default/Devices/GraphClient.json`. The device entry in the project file also repeats the `Address` of the device. If you change the `Address` (for example for the mock server), change it in **both** places and rebuild.

```json
"Variables": [
  { "Name": "GRAPH_TENANT_ID",   "DataType": "System.String", "Value": "<TENANT-ID>" },
  { "Name": "GRAPH_CLIENT_ID",   "DataType": "System.String", "Value": "<CLIENT-ID>" },
  { "Name": "GRAPH_SECRET_NAME", "DataType": "System.String", "Value": "GraphClientSecret" }
]
```

`GRAPH_SECRET_NAME` is the name of the secret of step 5b. Leave it unless you want another name.

**b) Client secret, in the secret database of the project.** **First set your own project vault key.** The project file ships a throw-away key that everybody who has this repository knows; a secret that you enter now would be protected by it. In the Designer open the project settings and change the project vault key to a new one (or use the setting that takes the key from the environment variable `HumanOSVaultKey`, see below). Then open `Secrets Database` (double click), *Add new Secret*:

| Field     | Value                                                                                  |
| :-------- | :------------------------------------------------------------------------------------- |
| Name      | `GraphClientSecret` (the value of `GRAPH_SECRET_NAME`)                                  |
| Type      | `UserPassword`                                                                          |
| User name | not used, can stay empty                                                                |
| Password  | the **client secret** of step 1 (the *Value*, not the *Secret ID*)                      |

The secret type `UserPassword` is the type of the *entry in the secret database*. The device property `OAuthSecretSourceType` = `Password` (in `GraphClient.json`) tells the OAuth login to read the client secret from such an entry (the password field, found by the name in `OAuthSecretSource`). The two belong together; leave both as they are.

The secret is stored encrypted in the project file and is written to `Secrets\` of the built target. The project uses `SecurityMode = StoreKeyInProject`, which means the key sits in the same file: **never commit a project file with a filled secret database.** Keep your working copy outside of any public repository, or remove the entry again before you commit. For more protection give the key through the environment variable `HumanOSVaultKey` (the target is prepared for it).

**c) Room id.** Open `default/Devices/RoomCalendars.json` (or the device `RoomCalendars` in the Designer). It has two room groups, `Room1` and `Room2`. Each has the property **`RoomId`**. Enter `<ROOM-OBJECT-ID>` for `Room1`:

```json
{ "Name": "RoomId", "DataType": "System.String", "Value": "<ROOM-OBJECT-ID>" }
```

A room with an empty `RoomId` is skipped (its `Status` says *Not configured*). To publish more rooms than two, see [Add another room](#add-another-room).

The Designer builds from the device file of the target (`default/Devices/RoomCalendars.json`), not from the template. Change the template as well if you create new devices from it.

## Step 6: Build and start the gateway

1. Open `ExchangeAgent.h2proj` in the Designer and **build** the target `default`. The target uses the trial runtime (`HumanOS.IoT.Runtime win-x86 TRIAL`, version 2.12); the output is in `Build\default\`. On the command line, with the HumanOS BuildTool:

   ```text
   HumanOS.BuildTool.exe "ExchangeAgent.h2proj" --build --target=default
   ```

   `HumanOS.BuildTool.exe` is not on the `PATH`. It is installed with the Designer (search the Designer's install folder for the file name) and has to be called with its full path. If you are unsure, build in the Designer. Check the result: `Build\default\` must hold `appsettings.json`, `Config\` and `Secrets\GraphClientSecret.json`.

   A licensed runtime also works. Then select it in the target and use the device licenses of your license: the id of a device must match its device license.

2. Start the gateway from the folder of the installed runtime. **No backslash at the end of the path.** The runtime appends it itself (the settings in the project join the folder and `Secrets\` without a separator), and a trailing backslash before the closing quote is read by Windows as an escaped quote (see [Troubleshooting](#troubleshooting)):

   ```text
   cd "C:\Program Files (x86)\CyberTech\HumanOS.IoT.Runtimes\HumanOS.IoT.Runtime-win.trial.x86-v2.12"
   .\HumanOS.IoT.Gateway.exe "-AppSettingsDir=<project folder>\Build\default"
   ```

   The runtime folder above is where the trial runtime 2.12 was installed on the author's machine. Yours may differ: use the folder that holds `HumanOS.IoT.Gateway.exe` of the runtime that your target selects.

3. The trial runtime shows its trial notice and **runs for one hour**. The log of a good start (Information level) contains:

   ```text
   Kernel: InfoModelManager: Schema file 'CalendarPolling.json' loaded.
   Kernel: Device [GraphClient@https://graph.microsoft.com/v1.0] detected.
   Kernel: Device [RoomCalendars@RAM] detected.
   OpcUaServer: Endpoint [opc.tcp://<computer name>:4840/ [None:None:Binary]]: Status = Good
   OpcUaServer: Server is running
   ```

   About 10 seconds after the start the timer fires for the first time:

   ```text
   ReadCalendars: Started.
   ReadCalendars: Room 'Room1': 3 event(s).
   ReadCalendars: Room 'Room2': no RoomId configured, skipped.
   ReadCalendars: Completed: 1 room(s) read, 0 failed, 1 not configured.
   ```

   The log files were found in `Build\default\Data\Logs` (the folder is created at the first start; the path comes from the project setting `Serilog...path` and may differ with your settings). The gateway also writes the log to its console window. Set `"AppConfig.LogMinimumLevel": "Debug"` in the `AppSettings` of the target for more detail (the window of the request, every page that is read).

### The local trigger

The command `ReadCalendars` does not run by itself. Something has to start it. A productive agent in a HumanOS Platform system can be triggered by the Platform through the shared node space; that way does **not apply to this example**. The **trial runtime has no shared node space** (and no Platform), so this example polls: it brings its own, local trigger, a timer rule that fires every 60 seconds:

- `default/DataModel/Objects/CalendarPolling.json` is one DataModel object that holds **the command, a timer and a rule**. A rule only sees nodes that are members of its own group, so the three belong to the same object.
- The timer has `"InitialDelay": "0.0:00:10.00"` and an interval of 60 seconds.
- The rule `RunReadCalendars` has `"TriggerEvent": "OnTimer"` and as action the script `ReadCalendarsTimer.cs`. The script looks up the command by its name in the node space and calls it with `TCommandHelper.call`.
- The command lets only one run at a time. If Graph is slow, a tick during a running read is skipped (Information log *The previous run is still active, skipped*).

To change the polling interval edit `Interval` and `IntervalUnit` of the timer. Note that Graph throttles an app that asks too often; one request per room and minute is far below the limits.

**Other triggers.** Anything that can call a command node works. A pattern that was used to start import commands once after the start of a trial runtime is a *workflow* that is started by a generic rule on `OnObjectAppeared`; its script waits some seconds and calls the command:

```json
{
  "Name": "StartRule",
  "Type": "GenericRule",
  "TriggerEvent": "OnObjectAppeared",
  "TriggerCondition": "Context.Node is IWorkflow && Context.Node.Name == \"MyTriggerWorkflow\"",
  "Action": {
    "Type": "Command",
    "Arguments": [ { "Name": "Node", "Predicate": "Node.getProperty<int>('TotalRuns') == 0" } ],
    "CommandExpression": { "Node": "$(Node)", "Arguments": [] }
  }
}
```

```csharp
// operation script of the activity of the workflow (TAbstractOperationScriptObject)
public override async Task runAsync(IKernelAccess Kernel, IActivity Activity, CancellationToken CancellationToken)
{
  await Task.Delay(10000, CancellationToken).ConfigureAwait(false);
  ICommandNode Command = await Kernel.NodeSpace.queryOrWaitForNodeLocallyAsync(
    n => n.Name == "ReadCalendars", TimeSpan.FromSeconds(30.0), CancellationToken) as ICommandNode;
  TCommandResult Result = await Task.Run(() => TCommandHelper.call(Command, new TCommandArgs())).ConfigureAwait(false);
}
```

This second variant is **not part of the project**. The command node must have a **different name** from the DataModel object that contains it: a lookup by name finds the object first and the cast to the command gives `null`.

## Step 7: Look at the result with an OPC-UA client

**First book the room**, otherwise the calendar is empty and you cannot tell "works" from "broken": in Outlook create an appointment of 30 minutes that **starts a few minutes ago** and invite the room (`example-room@<DOMAIN>`) as a resource; the room must accept it. Optional: a second appointment later today. Within a minute (the first run is 10 seconds after the start) `Room1` shows `EventCount` 1 and `Busy` `True`. With an empty room, `EventCount` 0, `Busy` `False` and `Events` `[]` are correct, too: they only prove that the login and the access work.

**Security notice.** The OPC-UA server of this example accepts an **anonymous login and the security mode `None`**. Everybody who reaches port 4840 can read the room data. Use the example only in a test network, or close the port (see [Limits](#limits-and-security-notes)).

1. Start UaExpert. *Server, Add*, double-click *Custom Discovery* and enter `opc.tcp://localhost:4840` (or `opc.tcp://<computer name>:4840` from another machine). The server lists its endpoints.
2. Select the endpoint with **Security Policy `None`**, authentication **Anonymous** and connect. The other endpoints (`Basic256Sha256`, `Aes128Sha256RsaOaep`, ... with *Sign* or *SignAndEncrypt*) need a certificate: the server creates its own certificate at the first start, and you confirm it as *trusted* in the client. The server uses `Config\HumanOS.PeSeL.OPCUAServer\CertificateStores` for it; the trial OPC-UA server works for one hour.
3. In the *Address Space* open `Root, Objects, Server, HumanOS, RoomCalendars`. Drag `Room1` and its variables into the *Data Access View*.

   Some clients list `RoomCalendars`, `Room1` and `Event01` to `Event10` **twice**. It is the same node with the same NodeId, a quirk of the server, and harmless.

**What you see** (an illustration with invented names and subjects; your values differ. UaExpert, *Address Space* on the left, *Data Access View* in the middle):

```text
Root
└─ Objects
   └─ Server
      └─ HumanOS
         └─ RoomCalendars
            ├─ Status                 "1 room(s) read, 0 failed, 1 not configured"
            ├─ LastRun                2026-11-06 06:26:29 (UTC)
            ├─ Room1
            │  ├─ Status              "OK"
            │  ├─ LastUpdate          2026-11-06 06:26:29 (UTC)
            │  ├─ EventCount          3
            │  ├─ Busy                True
            │  ├─ Events              [{"id":"AAMk...","subject":"3D printing basics","start":"2026-11-06T05:56:29Z", ...
            │  ├─ Event01
            │  │  ├─ Subject          "3D printing basics"
            │  │  ├─ Start            2026-11-06 05:56:29 (UTC)
            │  │  ├─ End              2026-11-06 07:26:29 (UTC)
            │  │  ├─ Organizer        "Alex Example"
            │  │  ├─ AttendeeCount    2
            │  │  └─ EventId          "AAMk..."
            │  ├─ Event02 ...
            │  └─ Event10
            └─ Room2 ...
```

In the *Data Access View* the variables of `Room1` show their value, a *Good* status and the time of the last change. A client that subscribes to the variables gets a notification whenever a poll changes a value.

The variables of the device:

| Variable                          | Type     | Meaning                                                                                                              |
| :-------------------------------- | :------- | :------------------------------------------------------------------------------------------------------------------- |
| `RoomCalendars/Status`            | String   | Result of the last run, e.g. `2 room(s) read, 0 failed, 0 not configured`                                             |
| `RoomCalendars/LastRun`           | DateTime | Time (UTC) of the last run                                                                                            |
| `<Room>/Status`                   | String   | `OK`, `Error: <reason>` or `Not configured: ...`. After an error the events of the last good run **stay** in the nodes |
| `<Room>/LastUpdate`               | DateTime | Time (UTC) of the last successful read of this room                                                                   |
| `<Room>/EventCount`               | Int32    | Number of events in the window (all of them, not only the ten slots)                                                  |
| `<Room>/Busy`                     | Boolean  | An event is running now (start before now, end after now). **Busy ignores `showAs`** (an event shown as *free* still counts) **and all-day events** (they count as well): such an event makes the room busy anyway |
| `<Room>/Events`                   | String   | **All** events of the window as a JSON array (see below)                                                              |
| `<Room>/EventNN/Subject`          | String   | Subject of the NN-th event (sorted by start, `Event01` is the running or next one); empty if the slot is not used     |
| `<Room>/EventNN/Start`, `/End`    | DateTime | Start and end (UTC). An unused slot shows `1601-01-01 00:00:00`, the OPC-UA zero time                                  |
| `<Room>/EventNN/Organizer`        | String   | Display name of the organizer                                                                                         |
| `<Room>/EventNN/AttendeeCount`    | Int32    | Number of invited **persons** (rooms and equipment are not counted)                                                   |
| `<Room>/EventNN/EventId`          | String   | Id of the event in Graph                                                                                              |

The JSON of the variable `Events`:

```json
[
  {
    "id": "AAMk...",
    "subject": "3D printing basics",
    "start": "2026-11-06T05:56:29Z",
    "end": "2026-11-06T07:26:29Z",
    "isAllDay": false,
    "organizer": { "name": "Alex Example" },
    "attendees": [ { "name": "Sam Sample", "response": "accepted" } ]
  }
]
```

The variables are **read only**, and until the first run they hold no value; a client shows the status *Bad* (`BadStateNotActive`) for them in the first seconds after the start. The OPC-UA node ids are the GUIDs of the nodes in the device file and do not change between builds.

## Try it without Microsoft 365 (mock server)

`MockGraph/mock_graph.py` answers the one request the agent sends (`users/{id}/calendar/calendarView`) with invented data that moves with the current time. It also splits the answer into pages and sets `@odata.nextLink`, so the paging is exercised.

```text
python MockGraph\mock_graph.py            # listens on http://127.0.0.1:8099/v1.0
```

| Room id                                  | Answer                                                                    |
| :--------------------------------------- | :------------------------------------------------------------------------ |
| `11111111-1111-1111-1111-111111111111`   | 13 events (one running now, one cancelled), pages of the requested size    |
| `22222222-2222-2222-2222-222222222222`   | 2 events                                                                   |
| `33333333-3333-3333-3333-333333333333`   | an empty calendar                                                          |
| `44444444-4444-4444-4444-444444444444`   | HTTP 403 `ErrorAccessDenied` (like a blocked access policy)                |
| any other id                             | HTTP 404 `ErrorInvalidUser`                                                |

Then change three things in the device files and **build the target again**:

1. `default/Devices/GraphClient.json`: `Address` to `http://127.0.0.1:8099/v1.0` and the property `OAuthEnable` to `false`. Change the `Address` of the device entry `GraphClient` in `ExchangeAgent.h2proj` to the same value. (With OAuth switched off the gateway does not ask for a token and does not need the secret: this was run with an empty `Secrets\` folder. A target that you built with the Designer holds the entry that you made in step 5b anyway.)
2. `default/Devices/RoomCalendars.json`: `RoomId` of `Room1` to `11111111-1111-1111-1111-111111111111`, of `Room2` to `22222222-2222-2222-2222-222222222222`.
3. Optional: `PageSize` of `RoomCalendars` to `5`, so that room 1 is read in three pages.

The mock has no authentication and the tokens are ignored: use it on `localhost` only. The log of the run with page size 5: `Reading page 2...`, `Reading page 3...` (Debug level), then `Room 'Room1': 12 event(s).`. Room 1 has more events than slots: `EventCount` is 12, `Event01` to `Event10` hold the first ten, the variable `Events` all twelve (the cancelled one is left out).

## How it works

**Reading.** `ReadCalendars` asks Graph for the window *now* to *now + `LookAheadHours`* (168 hours, seven days) of every room with a `RoomId`. The device command `GetCalendarView` sends

```text
GET users/<RoomId>/calendar/calendarView?startDateTime=...Z&endDateTime=...Z&$top=<PageSize>
    &$select=id,subject,start,end,isAllDay,isCancelled,organizer,attendees
Prefer: outlook.timezone="UTC"
```

`calendarView` expands recurring appointments into single occurrences and returns every event that overlaps the window. The header `Prefer: outlook.timezone="UTC"` makes Graph return all times in UTC. **Paging:** Graph returns at most `$top` events per answer (a small default applies if `$top` is absent). If more follow, the answer holds the absolute URL of the next page as `@odata.nextLink`. The script reads that link and calls the command `GetNextPage` with it (it replaces the address of the command), until an answer has no link (at most 100 pages per room and run).

**Writing the nodes.** The events are sorted by start time. Cancelled events are dropped. Rooms and equipment that are invited (attendee type `resource`) are not counted as attendees. The values are written into the data nodes of the `NoDevice` device `RoomCalendars` with `passValue(Value, false)`: the nodes live only in memory and are read only for OPC-UA clients. The default OPC-UA model of the server turns the device, its groups and its data nodes into the OPC-UA objects and variables of the tree above.

**Errors keep the old data.** If Graph answers with an error for one room (403, 404, a network problem), the `Status` of that room says so and the other rooms are still read. The events of the last good run stay in the nodes and `LastUpdate` stays old. A consumer should check `Status` or `LastUpdate` before it trusts the data, for example a door display should show *no data* when `LastUpdate` is older than a few minutes.

**What the OPC-UA server publishes.** The server publishes the device `RoomCalendars` only. The Graph client is hidden through the device filter in `HumanOS.PeSeL.OPCUAServer/settings.json`:

```json
"Models": [ { "Name": "Default", "RootBrowseName": "HumanOS",
              "DeviceFilter": "device.hasProperty<bool>(\"EnableOpcUa\", true)" } ]
```

Only devices with the property `EnableOpcUa = true` pass. The reason: the commands of a device appear as **OPC-UA methods**. If `GraphClient` were published, every OPC-UA client could call `GetCalendarView` with **any** user id and read every calendar the app may read. To try the methods during development give `GraphClient` the property `EnableOpcUa` = `true`; you can then call `GetCalendarView(UserId, From, To, PageSize)` from UaExpert and get the raw Graph answer in the output `Content`. (The manual for the OPC-UA server names the parameter of the filter `node`; the runtime 2.12 requires `device`. With `node` the filter cannot be evaluated and the log says *Could not evaluate device filter*.)

## Configuration reference

| Where                                        | Name                | Default      | Meaning                                                                                      |
| :------------------------------------------- | :------------------ | :----------- | :------------------------------------------------------------------------------------------- |
| device `GraphClient`, variable               | `GRAPH_TENANT_ID`   | `<TENANT-ID>`| Directory (tenant) id of Entra (`OAuthDirectoryId`)                                           |
| device `GraphClient`, variable               | `GRAPH_CLIENT_ID`   | `<CLIENT-ID>`| Application (client) id of the app (`OAuthClientId`)                                          |
| device `GraphClient`, variable               | `GRAPH_SECRET_NAME` | `GraphClientSecret` | Name of the secret with the client secret (`OAuthSecretSource`)                         |
| device `GraphClient`, property               | `OAuthEnableLogging`| `false`      | `true` logs the OAuth exchange (MSAL) for troubleshooting                                    |
| device `RoomCalendars`, property             | `LookAheadHours`    | `168`        | Length of the window that starts now                                                         |
| device `RoomCalendars`, property             | `PageSize`          | `50`         | Events per Graph page (`$top`). Set `1` or `2` to test the paging with a real calendar        |
| device `RoomCalendars`, property             | `IncludeMailAddresses` | `false`   | `true` adds `mail` of organizer and attendees to the JSON of `Events`                         |
| device `RoomCalendars`, property             | `EnableOpcUa`       | `true`       | Device filter of the OPC-UA server                                                           |
| device `RoomCalendars`, property             | `Endpoint`          | `Calendar`   | How the script finds the device. The Graph device has `Endpoint` = `Exchange`. Do not change  |
| group `Room1`, `Room2`, ..., property        | `RoomId`            | empty        | Object id of the room mailbox. Empty: the room is skipped                                     |
| group `Room1`, `Room2`, ..., property        | `Role`              | `Room`       | Marks a group as a room. Do not change                                                       |
| `CalendarPolling.json`, timer                | `Interval`          | `60` seconds | Polling interval; `InitialDelay` is the wait before the first run                            |
| `HumanOS.PeSeL.OPCUAServer/settings.json`    | `Port`              | `4840`       | OPC-UA port. Change it when another gateway on the machine already uses 4840                  |

### Add another room

Two rooms are prepared. For more, copy a room group with all its nodes (each node needs a new GUID as its id). The script does that for the device file and the template; close the project in the Designer first:

```powershell
.\Setup\Add-Room.ps1 -Name Room3 -RoomId <ROOM-OBJECT-ID>
```

Rebuild the target afterwards. The number of event slots (`Event01` to `Event10`) is the number of `EventNN` groups in the room group: the command fills the consecutive groups it finds. Remove the last ones for fewer slots; the variable `Events` always holds all events.

## Limits and security notes

- **Read only.** The agent reads calendars. **Creating, changing and deleting appointments through OPC-UA is not implemented.** It would need the application permission `Calendars.ReadWrite` (a different security level: the app could then change every calendar it can reach), Graph commands for `POST`, `PATCH` and `DELETE` on `events`, and OPC-UA methods or writable variables. That is a deliberate step you should take consciously. Restrict the app with the access policy first.
- **No shared node space, no Platform trigger.** The only trigger is the local `OnTimer` rule (polling every 60 seconds). A trigger by a Platform over the shared node space belongs to a productive agent and is neither used nor tested here.
- **Polling.** The calendars are polled every 60 seconds. A new booking is visible within a minute, not at once. Microsoft Graph also offers change notifications (webhooks); they need a public HTTPS endpoint and are not used here.
- **OPC-UA access is open.** The example server allows an anonymous login and the security mode `None`. The room data (subjects, organizers) is visible to everybody who reaches port 4840. For anything but a test set `DisableAnonymous` and `DisableBasicNoSecurity` in the settings of the OPC-UA server and create a user secret (see the manual of the OPC-UA server, *Secret Management*), restrict the port with a firewall, and keep `IncludeMailAddresses` at `false` if the clients do not need mail addresses.
- **Personal data.** Subjects, names of organizers and attendees are personal data. The agent writes no calendar data to disk. Be aware that the REST log of a device (property `UHAL:LogFileName`, empty in this project) would dump complete answers if you switch it on.
- **Secret expiry.** A client secret expires. Plan a rotation: create a new secret in Entra, enter it in the secret database (same name), rebuild or republish, restart the gateway, then delete the old secret.
- **The window.** Events that start after the window and events that ended before the start of the window are not listed. An event that is running now is.
- **Slots.** Only the first ten events per room appear as individual variables; the JSON in `Events` has all of them. `Events` is one text value: a window with many hundred events makes a large value.
- **Trial runtime.** It stops after one hour; restart it. Time zones: all times are UTC; a client may show them converted.
- **Paging.** The paging follows the link of the answer as the Graph documentation describes it. It was checked against the real test room with `PageSize` = 1 and three events (three pages) and with the mock server. Set `PageSize` to `1` for a room with two or more events to see it in your own directory.

## Test status

| Checked                                                                                                                     | How                                                                  |
| :-------------------------------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------- |
| Login, `calendarView`, the `Prefer` header, `$select`, paging (`PageSize` 1, three events) and the OPC-UA nodes             | Against the real test room (Microsoft 365), trial runtime 2.12.0.6882, with a hand-made build output; once with the room address as `RoomId`; `Busy` was `True` while a booking was running |
| Paging over three pages, HTTP 403 and 404, an empty calendar, three rooms, the OPC-UA methods of the Graph device          | Against the mock server                                              |
| The object id of the room (step 3) as `RoomId`                                                                              | **Not tried** against a real tenant. Only the mail address of the room was used there; the code treats both the same (the value goes into the URL) |
| Dropping of cancelled events                                                                                                | Against the mock server only. The real room removes a cancelled meeting from its calendar, so Graph did not return it and the filter was not reached |
| All four scripts compile against the 2.12 assemblies                                                                        | Compiler run with the assemblies of the runtime                      |
| `Setup/Add-Room.ps1`                                                                                                        | On a test copy of the project                                        |
| `Setup/New-GraphApp.ps1`, `New-RoomMailbox.ps1`, `Set-AccessPolicy.ps1`                                                     | **Not run in exactly this form** (derived from commands used before) |
| The build in the Designer and the replacement of the `$(GRAPH_*)` variables by the Designer or the BuildTool                | **Not checked.** The build output was made by hand from the files of the project (device files with the variables replaced, the secret entry copied, the project vault key put into `appsettings.json`). The Designer menus of steps 5 and 6 were not clicked through |
| Entering a secret through the Designer and building it into `Secrets\`                                                      | A secret entered in the Designer was copied by hand into the build output and used for the real login: the login worked          |

## Troubleshooting

| Symptom                                                                                                  | Cause and remedy                                                                                                                                                                                                                       |
| :------------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Log: `Loading device [GraphClient@...] failed. Could not find secret 'GraphClientSecret'.` and, at the first tick, `Unable to find the Graph device, it must have the property 'Endpoint' with value 'Exchange'.` | The secret is not in `Build\default\Secrets\`. Create it in the secret database (step 5b), name exactly as `GRAPH_SECRET_NAME`, and **build again** (secrets are written at the build).                                  |
| The gateway starts, but the log shows the folder of the runtime as `AppSettingsDir`, or complains about a secrets path in the runtime folder | The argument ended with a backslash: Windows reads `\"` as an escaped quote. Write `"-AppSettingsDir=<folder>\Build\default"` without a trailing backslash.                                                          |
| Room `Status`: `Error: GetCalendarView failed: HTTP 403 ErrorAccessDenied: ...`                          | The app has no access to this mailbox. Check: admin consent given (*Granted*)? The permission is `Calendars.ReadBasic.All` as an **application** permission? The room is a member of the access-policy group, and has the policy had time (up to about one hour) to take effect? |
| Room `Status`: `Error: ... HTTP 404 ErrorInvalidUser`                                                    | The `RoomId` is wrong. It must be the object id of the room mailbox (`ExternalDirectoryObjectId`, step 3), not the id of the place. (Graph also accepts the address of the room mailbox; the object id is the stable choice.)                                                                       |
| Log or status with an `AADSTS...` code, or `HTTP 401`                                                    | Entra refuses the login. `AADSTS7000215`: invalid client secret (copied the *Secret ID* instead of the *Value*, or a typo). `AADSTS7000222`: the secret has expired, create a new one. `AADSTS700016`: no app with this client id in the directory. `AADSTS90002`: the tenant id is wrong. Set `OAuthEnableLogging` to `true` on `GraphClient` to log the whole exchange. |
| `Status`: `Not configured: the property RoomId of this group is empty or still a placeholder`            | Enter the `RoomId` (step 5c) and build again. A value that starts with `<` counts as a placeholder.                                                                                                                                    |
| Log or status with `AADSTS900023`, `AADSTS90002`, `AADSTS700016` or a URL containing `<TENANT-ID>` or `<CLIENT-ID>` | The placeholders of the variables `GRAPH_TENANT_ID` / `GRAPH_CLIENT_ID` (step 5a) are still in the project, or the target was not built again after you changed them. The agent does not check these two values itself.                    |
| Events appear, but the subjects are empty                                                                | The room deletes the subject. Run `Set-CalendarProcessing -Identity <ROOM-MAIL> -DeleteSubject $false -AddOrganizerToSubject $false` (step 2). New bookings are fine after that, existing ones may keep the empty subject.            |
| An event does not appear                                                                                 | Is it inside the window (`LookAheadHours`)? Is it cancelled (cancelled events are dropped)? Did the room accept the booking (the room calendar must hold it: look at it in Outlook)? Right after a change or an invitation the room calendar was once seen empty for a short time; the cause is not known. If something looks wrong, check the room calendar in Outlook first. |
| `ReadCalendars` runs, but the variables have the status `Bad` (`BadStateNotActive`) in the client        | The variables have no value before the first successful read of the room. Wait for the first tick (10 s), then refresh the client.                                                                                                     |
| Log: `Could not evaluate device filter ...`                                                              | The filter of the OPC-UA server uses `node`; the runtime 2.12 expects `device` (see [How it works](#how-it-works)).                                                                                                                    |
| Log: `Unable to find the Graph device ...` / `... the calendar device ...` in a project you adapted       | The script finds the two devices by the property `Endpoint` (`Exchange` and `Calendar`). Keep both.                                                                                                                                    |
| Log: `ReadCalendarsTimer: the command 'ReadCalendars' was not found.`                                    | A node with the **name** `ReadCalendars` exists, but it is not the command (for example a DataModel object with that name). Rename the object.                                                                                         |
| Log: `Data item is read only.` in your own script                                                        | A script writes a read-only data node with `passValue(Value)`. Use `passValue(Value, false)` as the command does (a value that comes from the source of the node, not from a client).                                                  |
| Log: `Compile C# script ...` with errors                                                                 | The scripts are compiled at the start by the runtime. The message names the line. Command scripts must derive from `TAbstractCommandScriptObject2` (Runtime 2.11 or newer).                                                           |
| UaExpert cannot connect or says the certificate is not trusted                                           | Use the endpoint with *Security Policy None*, or confirm the server certificate as trusted. Try `opc.tcp://localhost:4840`, the endpoint URL contains the computer name. Port 4840 in use by another gateway: change `Port` in the OPC-UA settings. |
| Nothing is updated after an hour                                                                         | The trial runtime stopped after one hour. Start it again.                                                                                                                                                                             |

## Documentation

- WebControl driver, OAuth properties and logging: https://doc.cybertech.swiss/runtime/2.12/Manuals/HumanOS.UHAL.WebControl/DriverConfiguration
- OPC-UA server, models and device filter: https://doc.cybertech.swiss/runtime/2.12/Manuals/HumanOS.PeSeL.OPCUAServer/Models
- NoDevice driver: https://doc.cybertech.swiss/runtime/2.12/Manuals/HumanOS.UHAL.NoDevice/HumanOS.UHAL.NoDevice
- Timers and rules: https://doc.cybertech.swiss/runtime/2.12/Manuals/HumanOS.Runtime/GenericInformationModelNodes/Timers
- Microsoft: [List calendarView](https://learn.microsoft.com/graph/api/user-list-calendarview), [paging](https://learn.microsoft.com/graph/paging), [limit app access to specific mailboxes](https://learn.microsoft.com/graph/auth-limit-mailbox-access)
