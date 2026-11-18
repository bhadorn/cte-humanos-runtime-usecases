---
id: webservice-webui
title: "WebService WebUI"
subject: "Operating a JSON simulator with scripted commands from a WebUI served by the HumanOS WebService plugin"
keywords: [HumanOS, WebService, WebUI, wwwroot, static files, REST, FileReader, JSON simulator, command script, whitelist]
---

# WebService WebUI

Shows how an agent serves its own small user interface. The `HumanOS.PeSeL.WebService` plugin delivers an HTML page
from the folder `wwwroot` on the same port as its REST API. The page reads two data nodes of a simulated machine and
executes two C# command scripts, one of which writes a value and both of which return output arguments.

No hardware is required: the machine is a JSON file read and written by the FileReader connector.

## Architecture

```text
Browser  http://localhost:8080/
   │  GET  /index.html, /style.css, /fonts/*        static files (wwwroot)
   │  GET  /nodes/data?id=<data node id>           every 2 s
   │  POST /nodes/command?id=<command id>           on button click
   ▼
HumanOS.PeSeL.WebService (port 8080, WebRoot enabled)
   │
   ├─ Device "Machine" (FileReader)  ◄──►  C:\Temp\HumanOS.WebUI\Machine.json
   │     Temperature  $.Temperature  read
   │     Setpoint     $.Setpoint     read / write
   │
   └─ Global commands (DataModel/Objects/MachineCommands.json)
         SetSetpoint(Setpoint)  → Result           writes Setpoint into the JSON file
         GetMachineState()      → State, Deviation reads both values, returns Heating / Cooling / At setpoint
```

## Files

| **File**                                              | **Content**                                                     |
| ----------------------------------------------------- | --------------------------------------------------------------- |
| `WebServiceWebUI.h2proj`                              | IoT Designer project, target `default`                          |
| `Simulator/Machine.json`                              | Start values of the simulated machine                           |
| `DeviceTemplates/JsonMachine.json`                    | FileReader template with the data nodes Temperature, Setpoint   |
| `default/Devices/Machine.json`                        | Device reading `C:\Temp\HumanOS.WebUI\Machine.json`             |
| `default/DataModel/Objects/MachineCommands.json`      | Command nodes SetSetpoint and GetMachineState                   |
| `default/Scripts/Commands/SetSetpoint.cs`             | Checks the range 5 – 40 °C and writes the setpoint              |
| `default/Scripts/Commands/GetMachineState.cs`         | Compares temperature and setpoint (tolerance 0.5 °C)            |
| `default/HumanOS.PeSeL.WebService/settings.json`      | Instance on port 8080 with `WebRoot` enabled                    |
| `default/HumanOS.PeSeL.WebService/wwwroot/index.html` | The page; the node and command ids are in `CONFIG` at the top   |
| `default/HumanOS.PeSeL.WebService/wwwroot/style.css`  | CyberTech style guide: colour tokens light/dark, IBM Plex       |
| `default/HumanOS.PeSeL.WebService/wwwroot/fonts/`     | IBM Plex Sans and Sans Condensed (woff2, SIL Open Font License) |

## Prerequisites

- HumanOS IoT Designer and Runtime **2.13** or newer (the `WebRoot` setting of the WebService plugin is new in 2.13)
- A browser on the agent host

## Run

1. Copy `Simulator/Machine.json` to `C:\Temp\HumanOS.WebUI\Machine.json`.
2. Open `WebServiceWebUI.h2proj` in the IoT Designer, build the target `default` and run it.
3. Open `http://localhost:8080/`.

The page shows temperature and setpoint and refreshes them every 2 seconds. **Set setpoint** writes the new value
into the JSON file and shows the confirmation of the script. **Read state** shows the state returned by the agent.
Change `Temperature` in the JSON file with an editor: the page shows the new value within 2 seconds.

## REST Calls of the Page

| **Call**                                                                | **Body**                                                             |
| ----------------------------------------------------------------------- | -------------------------------------------------------------------- |
| `GET /nodes/data?id=f39b74d4-5aad-4368-8560-70984a31d469` (Temperature) | –                                                                    |
| `GET /nodes/data?id=988fc706-9252-4a11-9757-0f37ea079c4b` (Setpoint)    | –                                                                    |
| `POST /nodes/command?id=7d85a20a-9a01-4323-b3b9-c2298b4c6bb4`           | `<Argument name="Setpoint" datatype="System.Double">22.5</Argument>` |
| `POST /nodes/command?id=c53ec28a-3a07-4450-a6f5-b7d836bb6563`           | no argument                                                          |

All calls use `text/xml`; the command body is wrapped in `<HumanOS.Stream><Command>…</Command></HumanOS.Stream>`.
The answer of a command carries the output arguments, `ReturnCode` and `Exception`. A setpoint outside 5 – 40 °C
returns `BadArguments` with the reason in `Exception`.

## Locking the Instance Down

Without `AcceptedCalls` every request is allowed. To allow only what the page needs, set the whitelist of the
instance in `default/HumanOS.PeSeL.WebService/settings.json`:

```json
"AcceptedCalls": [
  "GET /index.html",
  "GET /style.css",
  "GET /fonts/IBMPlexSans-Light.woff2",
  "GET /fonts/IBMPlexSans-Regular.woff2",
  "GET /fonts/IBMPlexSans-SemiBold.woff2",
  "GET /fonts/IBMPlexSansCondensed-Light.woff2",
  "GET /fonts/IBMPlexSansCondensed-Regular.woff2",
  "GET /nodes/data?id=f39b74d4-5aad-4368-8560-70984a31d469",
  "GET /nodes/data?id=988fc706-9252-4a11-9757-0f37ea079c4b",
  "POST /nodes/command?id=7d85a20a-9a01-4323-b3b9-c2298b4c6bb4",
  "POST /nodes/command?id=c53ec28a-3a07-4450-a6f5-b7d836bb6563"
]
```

The page then works as before, `/` is checked as `/index.html`. Every other file (e.g. `/fonts/LICENSE.txt`) and
every other REST call (e.g. `GET /nodes/data`) returns `403 Forbidden`.

## Troubleshooting

| **Symptom**                     | **Cause**                                                                            |
| ------------------------------- | ------------------------------------------------------------------------------------ |
| Page returns `404`              | Runtime older than 2.13, or `WebRoot.Enabled` is `false`                             |
| Values stay at `–`              | `C:\Temp\HumanOS.WebUI\Machine.json` missing; see the FileReader messages in the log |
| Command returns `BadProcessing` | Device `Machine` or its data nodes renamed; the scripts look them up by name         |
| Port 8080 already in use        | Change `Port` in the WebService `settings.json`; the page uses relative URLs only    |

## See Also

- [WebUI](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.WebService/WebUI.md)
- [WebService Plugin Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.WebService/)
- [FileReader Data Access](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FileReader/DataAccess.md)
