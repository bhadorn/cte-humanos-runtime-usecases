---
id: usecase-file-reader-excel
title: "Use Case: Excel-Reader (FileReader)"
subject: "Reading an Excel file with the FileReader and syncing its orders into ctProduction maintenance jobs"
keywords: [HumanOS, FileReader, Excel, xlsx, ctProduction, maintenance job, pipeline, sync, import]
---

# Excel-Reader (FileReader)

Shows how the HumanOS **FileReader** reads an Excel file (`xlsx:` addresses) and how a script turns the rows into **maintenance jobs in ctProduction**. A row is an operation of a maintenance order. The sync creates jobs for new rows, updates jobs that are still in the file and cancels jobs whose row disappeared.

The project is called **FileReader** and holds one target per file format. The target **`Excel-Reader`** is described here; everything of this example (device, agent and skill, script, plugin settings) lives in that target, the device comes from the device template `EXCEL-READER`. The target `default` is not part of this example.

## Architecture

```text
MaintenanceOrders.xlsx  (edited by hand, e.g. in Excel)
        │  FileReader, address xlsx:$.Tabelle1  (read only)
        ▼
HumanOS gateway ── device ExcelReader ── data node SheetData (whole sheet as JSON array)
        │
        │  ctProduction action "sync" on MaintenanceJob  ──►  pipeline Pipeline.MaintenanceSync.Sync
        ▼
script Platform.MaintenanceSync.cs  (skill MaintenanceSync)
        │  compares the sheet with the existing MaintenanceJob records
        ▼
ctProduction: create / update / cancel maintenance jobs
```

## Prerequisites

- A licensed HumanOS IoT Runtime with Excel support in the FileReader: version **2.12.0.6863 or newer**. A trial runtime reads the Excel file but cannot join the platform (it logs "Shared nodespace is not supported") and expires one hour after it was started
- HumanOS IoT Designer (or the command line `HumanOS.BuildTool`) to build the target
- A ctProduction **test** tenant, a user with the rights to run the `sync` action, and the configuration of the folder [`ctProduction`](./ctProduction/Readme.md) applied to it
- Microsoft Excel to change the demo file

## Quick Start

The whole example in seven steps; each step is described further down or in the linked Readme.

1. Start a ctProduction test platform and note the code and the id of your tenant.
2. Import the seven files of [`ctProduction/`](./ctProduction/Readme.md) in the order of their numbers (set your tenant id in file 05 first).
3. Open `FileReader.h2proj` in the Designer, target `Excel-Reader`: enter the shared node space and the `ServiceId` of your platform, then build (see [Build and Run](#build-and-run)).
4. Start the gateway from the build folder and wait for `Successfully connected to the master controller` and `Actor 'Actor' registered` in its log.
5. In the platform open the list `Task`, select `Task.MaintenanceJob.ExcelSync` and press `Run`.
6. Check the result: 40 jobs, see [Check the result](#check-the-result).
7. Change `MaintenanceOrders.xlsx`, save it, press `Run` again: the steps of the [Demo](#demo).

## Key Concepts

- **One fixed file.** The FileReader watches exactly one file name (`MaintenanceOrders.xlsx`, next to `FileReader.h2proj`). The file must exist when the gateway starts. It does not watch a folder for new or dated files.
- **Read only.** Excel addresses are read-only; the gateway never changes the workbook.
- **The whole sheet as one value.** The address `xlsx:$.Tabelle1` returns every row of the sheet `Tabelle1` as a JSON array. The first row holds the column names, which become the keys. A repeated column name gets the suffix `_2` (the file has two columns `Bezeichnung`). Numbers and dates arrive as text, dates in ISO 8601.
- **Relative path.** The device address `..\..\..\MaintenanceOrders.xlsx` is resolved against the data folder of the built gateway (`Build\Excel-Reader\Data`), which leads back to the project folder. This holds for a gateway started from the built folder as described below; for a gateway installed somewhere else set the device address to the absolute path of the Excel file.
- **Identity of a job.** `Auftrag` (order) + `Vorgang` (operation) form the key, stored as `OrderNumber` + `Operation`. The columns `Auftrag`, `Personalnr`, `Vorgang` and `Equipment` are formatted as text: keep them text, because a number typed into a general cell loses its leading zeros (`0010` becomes `10`, which is another operation).
- **Soft delete.** A job whose row is gone gets `IsCancelled = true`; nothing is deleted. A cancelled or finished job is final: it is never reactivated, and a row for it is ignored.
- **Which jobs the sync manages.** Jobs that belong to a maintenance task (generated from the maintenance schedule) are never touched. Every other job with an order number and an operation is managed by the sync: if its row is not in the Excel file the job is cancelled. Use a test tenant.
- **Equipment must be known.** A new job needs a workplace. The column `Equipment` must equal the `InventoryNumber` of a `FacilityComponent` (the demo data provides `EQ-DEMO-01` to `EQ-DEMO-08`); otherwise the row is dropped and logged.
- **Empty sheet.** A sheet without a usable row stops the sync with an error instead of cancelling every job.

## Project Contents

| Path                                                   | Purpose                                                          |
| :----------------------------------------------------- | :--------------------------------------------------------------- |
| `FileReader.h2proj`                                    | Designer project, targets `default` and `Excel-Reader`           |
| `MaintenanceOrders.xlsx`                               | Demo data: 40 operations of 8 orders, sheet `Tabelle1`           |
| `Excel-Reader/Devices/ExcelReader.json`                | Device `ExcelReader` of the target, created from the template     |
| `Excel-Reader/Scripts/Workflows/Platform.MaintenanceSync.cs` | The sync script run by the ctProduction pipeline       |
| `DeviceTemplates/EXCEL-READER.json`                    | Device template with the data node `SheetData`, the agent `Actor` and the skill `MaintenanceSync`; the device inherits them |
| `ctProduction/`                                        | Import files for ctProduction, see its [Readme](./ctProduction/Readme.md) |

The Excel columns are: `Auftrag`, `Auftrag Bezeichnung`, `Kurztext`, `Personalnr`, `Istarbeit`, `Einheit Arbeit`, `VrgArbeitsplatz`, `Auftragsart`, `Erfassungsdatum`, `Start Uhrzeit`, `Vorgang`, `Langtext vorh.`, `Priorität`, `Techn. Platz`, `Bezeichnung`, `Equipment`, `Bezeichnung`, `Kostenstelle`, `Erfasser`. The sync uses `Auftrag`, `Auftrag Bezeichnung`, `Kurztext`, `Vorgang`, `Equipment`, `Erfassungsdatum`, `Istarbeit` and `Einheit Arbeit`; keep the headers and keep `Erfassungsdatum` a date. A job gets these values:

| Job field                      | From                                                                                   |
| :----------------------------- | :------------------------------------------------------------------------------------- |
| `Name`                         | `Auftrag/Vorgang Kurztext`, e.g. `9100001/0010 Visual inspection` (the order description if `Kurztext` is empty) |
| `OrderDescription`             | `Auftrag Bezeichnung`                                                                  |
| `EntryDate`, `SetStartDate`    | `Erfassungsdatum` (start date)                                                         |
| `DueDate`                      | Last day of the month of `Erfassungsdatum`, 23:59:59                                   |
| `SetDuration`                  | `Istarbeit` in the unit of `Einheit Arbeit` (`H`, `MIN` or `S`; no unit means hours). The file has no planned work, so the work of the row is used. Not written when empty or not a number |

A date typed into a cell that is not formatted as date arrives as a number; the sync converts it. A text that is not a date is not written (the job is still created or updated) and the pipeline log names the order.

## Build and Run

1. Import the ctProduction configuration ([ctProduction/Readme.md](./ctProduction/Readme.md)).
2. Connect the target to the platform. The platform and the gateways talk over a shared node space (MQTT). In the Designer open the target `Excel-Reader`, tab `HumanOS Kernel`, and set the **Shared Node Space** to the MQTT message bus of your platform (type `HumanOS.PeMiL.MQTTMessageBus.TSharedNodeSpaceHub`, the address of the broker) and enter the service id of your platform as master system controller and as master agent orchestrator (both the same value). It is the `ServiceId` of the platform web server (`HOS.WebServer`); the platform logs it at start (`ServiceId : {...}`), and your other ctProduction agents use the same value. The gateway log shows `No shared nodespace configured.` as long as this is not set. They are empty in this project, because they belong to your installation.
3. Build the target `Excel-Reader` in the Designer, or on the command line:

   ```text
   HumanOS.BuildTool.exe "FileReader.h2proj" --build --target=Excel-Reader
   ```

   The target needs a licensed runtime, see the prerequisites.
4. Start the gateway. It registers itself and its actor at the platform; nothing has to be published. For a local test the gateway (`HumanOS.IoT.Gateway.exe` in the folder of the installed runtime) can be started directly with the built folder:

   ```text
   HumanOS.IoT.Gateway.exe -AppSettingsDir="<project folder>\Build\Excel-Reader"
   ```

   About five seconds after the start the gateway has read the Excel file (the log shows `File watcher started on file ...MaintenanceOrders.xlsx`; the line `Excel content model successfully updated the content` appears only with the log level `Verbose`, set `AppConfig.LogMinimumLevel` in the target). With the platform connection the log also shows `Successfully connected to the master controller` and `Actor 'Actor' registered`. The OPC-UA server plugin of the target listens on port 4840; switch it off in the target if another gateway on the same machine already uses that port.

## Demo

All steps change `MaintenanceOrders.xlsx` (save the file, you can keep it open in Excel) and then start the sync again. The gateway picks up a saved file within a second; wait a moment after saving before you start the sync.

Start the sync with a call to the ctProduction REST interface (the same call can be made from any REST client). It answers with the id of the started pipeline:

```text
POST https://<ctProduction host>/api/v2/<tenant code>/entities/executeaction
Authorization: Bearer <token>
Content-Type: application/json

{ "ActionName": "sync", "CollectionId": "b42394d1-130a-485c-8a7e-7bfd82396a08" }
```

Instead of the REST call you can start the sync in the portal: import the task of file `07` ([ctProduction/Readme.md](./ctProduction/Readme.md)), select it in the task list and press `Run`.

The pipeline needs the gateway to answer for its skill. The platform asks every registered agent in turn, so an agent that is registered but not running makes the sync fail with `hasSkill failed: Timeout` (start all agents of the platform, or restart the platform to clear the stale registrations).

| Step | What you change in the Excel file                                                  | What the sync does                                                       |
| :--- | :--------------------------------------------------------------------------------- | :----------------------------------------------------------------------- |
| 1    | Nothing, run the sync                                                              | Creates the 40 jobs (8 orders, 5 operations each)                        |
| 2    | Change `Erfassungsdatum` of order `9100001`, operation `0010`                      | Updates start date and due date (end of the month) of that job           |
| 3    | Change `Kurztext` (and/or `Auftrag Bezeichnung`) in one row of order `9100001`     | Updates the name of that job (`Kurztext`, visible in the list) and its field `OrderDescription` (`Auftrag Bezeichnung`) |
| 4    | Add a row: order `9100009`, operation `0010`, equipment `EQ-DEMO-01`, a date       | Creates a new job                                                        |
| 5    | Delete a row                                                                       | Cancels that job (`IsCancelled`), it is not deleted                      |
| 6    | Add the deleted row again                                                          | The job stays cancelled and the row is ignored. Use a new order number to get a new job |
| 7    | Add a row with an `Equipment` that is not a demo component                         | The row is dropped, the pipeline log names the reason                    |

The result is visible in the list of maintenance jobs of ctProduction (new jobs with name, start date and due date, cancelled jobs). Every sync writes name, dates, duration and description of all jobs that are still in the file again, whether they changed or not.

A job that is done (state 900) is final like a cancelled one: a row for it is ignored. Starting a job in ctProduction needs a workplace agent for the workplace of the job; the demo workplace has none, so the jobs of this example are not started.

## Check the result

**Logs.** After a sync the gateway log shows `OPERATION for Job 'SyncJob'`, `Trigger: sync`, `Maintenance sync: N MaintenanceJob change(s) from M Excel row(s).` and `Job successfully finished.` Rows that are skipped or dropped (unknown equipment, no date, duplicates, final jobs) are named in the log. The platform log shows the activity `SyncJob` assigned to the actor `Actor`.

**Jobs.** After step 1 the list of maintenance jobs holds these values (the details view shows `Name`, the planning fields and the order number; `OrderDescription` is a field of the job that the standard views do not show):

| Check               | Expected                                                                                   |
| :------------------ | :----------------------------------------------------------------------------------------- |
| Count               | 40 jobs, 8 orders, operations `0010` to `0050`, none cancelled                             |
| `Name`              | `<Auftrag>/<Vorgang> <Kurztext>`, e.g. `9100001/0010 Visual inspection`                    |
| `SetStartDate`      | `Erfassungsdatum` of the row                                                               |
| `DueDate`           | last day of that month, 23:59:59                                                           |
| `SetDuration`       | `Istarbeit` in hours: eight jobs each with 00:30, 00:45, 01:00, 01:30 and 02:00             |
| `WorkplaceId`       | the demo workplace                                                                         |

The same list can be read through the REST interface (read only; the answer holds `entities`, the job fields are in `fields`):

```powershell
# only for a development platform with a self-signed certificate:
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
$Hdr  = @{ Authorization = "Bearer $env:HOS_TOKEN" }
$Body = '{"CollectionId":"b42394d1-130a-485c-8a7e-7bfd82396a08","Count":200,"Offset":0}'
$Jobs = (Invoke-RestMethod -Method Post -Uri "https://<platform host>/api/v2/<tenant code>/entities/queryentitylist" -Headers $Hdr -ContentType 'application/json' -Body $Body).entities | ForEach-Object { $_.fields }
$Jobs | Sort-Object OrderNumber, Operation | Select-Object Name, SetStartDate, DueDate, SetDuration, IsCancelled | Format-Table -AutoSize
```

## Troubleshooting

| Symptom                                                         | Cause and remedy                                                                                              |
| :-------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------ |
| Gateway log `No shared nodespace configured.`                   | Shared node space or `ServiceId` of the platform is not set in the target. With a trial runtime the log says `Shared nodespace is not supported`: use a licensed runtime |
| Sync fails with `hasSkill failed: Timeout`                      | The platform asks every registered agent in turn; an agent that is registered but not running breaks it. Start all agents of the platform or restart the platform |
| Sync fails with `No actor matching skill '...' found`           | The skill id of the agent does not match. The id `9cf292c6-e00b-417f-84e3-00ff754f9ab8` must be the same in the skill, in the `Skills` list and in the property `Skills` of the agent (the Designer can rewrite these references when it saves the project) and in `SkillId` of the pipeline template (file 04) |
| Starting a job fails with `No actor matching skill '{<workplace id>}'` | Not part of the sync. A job is started by a workplace agent that serves the workplace; the demo workplace has none |
| Imported workplace is not in the list                           | `Workplace` belongs to a tenant. Set `OwnerTenantId` in file 05 to your tenant id and import again             |
| Import answers `Data does not match the selected entity model`  | The file is imported into the wrong list; the `CollectionId` of the file must be the collection of the list    |
| `ActionConfiguration` is not among the fields of `MaintenanceJob` | It is a property of the model, not a field. Import file 03 as described in the [Readme of `ctProduction/`](./ctProduction/Readme.md) |
| Task `Task.MaintenanceJob.ExcelSync` is not in the task list    | Only active tasks are listed. File 07 creates it active; import the current file again                        |
| A job has no dates                                              | `Erfassungsdatum` is not a date. The sync converts Excel numbers, anything else is logged and not written     |
| `Maintenance sync` is not in the log, an old script runs        | The gateway compiles the script when it starts. Build the target again and restart the gateway                |
| Sync stops with `no usable data rows`                           | The sheet is empty or holds only malformed rows. This is intended: it keeps a half-written file from cancelling every job |
| Gateway cannot start, port 4840 is in use                       | The OPC-UA server plugin of the target needs the port; stop the other gateway or switch the plugin off        |

## Limits

- One fixed file name, existing at gateway start. A file that is saved under a new name is not noticed.
- The sync runs when the `sync` action is called (REST, the task `Run` button or a ctProduction script); it does not run by itself unless you give the task a shorter trigger.
- The gateway compiles the sync script when it starts; restart the gateway after you change the script.
- The `sync` action and its pipeline are delivered as import files in `ctProduction/` and are not part of the standard ctProduction model yet. Import them into a test tenant first.
