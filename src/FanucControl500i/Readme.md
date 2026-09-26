---
id: usecase-fanuc-control-500i
title: "Use Case: FANUC 500i-A Control (FOCAS3)"
subject: "Connecting a new-generation FANUC 500i-A CNC controller over the authenticated FOCAS3 protocol and publishing its data via OPC-UA"
keywords: [HumanOS, FANUC, 500i-A, FOCAS3, CNC, OPC-UA, NCGuide, CNC GUIDE 2, credentials, secrets, FanucControl]
---

# FANUC 500i-A Control (FOCAS3)

Shows how to connect a **new-generation FANUC Series 500i-A controller** to a HumanOS IoT gateway using
the **FOCAS3** interface and publish the machine data through the HumanOS OPC-UA server.

Unlike the classic FOCAS2 connection used by the older FANUC examples (e.g. address `NCGUIDE!`),
FOCAS3 is a TCP-based protocol that **requires a user login** on the controller. The protocol is selected
by the `focas3://` address scheme, and the username/password is taken from the project's encrypted
secret store (`FanucCredentials`), so no credentials are stored in plain text in the device configuration.

The project is pre-configured for the **FANUC CNC GUIDE 2** simulator running locally
(`focas3://127.0.0.1:8193`), so no physical CNC hardware is required.

## Architecture

```text
  FANUC Controller                        HumanOS IoT Runtime (Gateway)                     OPC-UA Client
┌───────────────────┐                 ┌───────────────────────────────────────────┐    ┌──────────────────┐
│  FANUC 500i-A      │   FOCAS3        │  HumanOS.UHAL.FanucControl                 │    │                  │
│  (CNC GUIDE 2      │  TCP :8193      │    login with secret "FanucCredentials"    │    │  UAExpert,       │
│   simulator or     │◀──────────────▶│             │                              │    │  SCADA, MES, …   │
│   real control)    │  user/password  │             ▼                              │    │                  │
└───────────────────┘                 │  Device: FanucControl (FanucControl500i_v1)│    │                  │
                                       │    ├─ Controller (state, times, counters) │    │                  │
                                       │    │   └─ NCPath1 (program, feed, axes)   │    │                  │
                                       │    ├─ MachineAlarming (NC alarms)         │    │                  │
                                       │    └─ DataProcessors (override %)         │    │                  │
                                       │             │                              │    │                  │
                                       │             ▼                              │    │                  │
                                       │  HumanOS.PeSeL.OPCUAServer  :4840  ───────┼──▶│ opc.tcp://…:4840  │
                                       └───────────────────────────────────────────┘    └──────────────────┘
```

## Project Layout

| Path                                              | Purpose                                                                                     |
| :------------------------------------------------ | :------------------------------------------------------------------------------------------ |
| `FanucControl500i.h2proj`                         | HumanOS IoT Designer project file (target, plugins, device instance, secret database)       |
| `DeviceTemplates/FanucControl500i_v1.json`        | Device template: FANUC 500i-A data nodes, alarm pool, override processors and `SecretName` variable |
| `default/`                                        | Default IoT Gateway target                                                                  |
| `default/Devices/FanucControl.json`               | Concrete device instance (address `focas3://127.0.0.1:8193`, type `500i-A`, machine `CNC GUIDE 2`) |
| `default/HumanOS.UHAL.FanucControl/settings.json` | FANUC driver: task processors and memory mappings (dynamic, modal, tool, alarm data)        |
| `default/HumanOS.PeSeL.OPCUAServer/settings.json` | OPC-UA server on port `4840`, root browse name `HumanOS`                                    |
| `default/HumanOS.UHAL.DeviceDetectors/`           | Device-detector plugin (enabled)                                                            |
| `globalids.json`                                  | Stable global node ids for the published data model                                         |
| `Build/`                                          | Output of *Publish* for the `default` target (not versioned; incl. service scripts and `Secrets/`) |

## FOCAS3 Connection and Credentials

| Setting                | Value                       | Meaning                                                                         |
| :--------------------- | :-------------------------- | :------------------------------------------------------------------------------ |
| `Address`              | `focas3://127.0.0.1:8193`   | The `focas3://` scheme selects the FOCAS3 protocol; host and TCP port of the CNC |
| `Type`                 | `500i-A`                    | FANUC controller series                                                         |
| `MachineName`          | `CNC GUIDE 2`               | Display name of the machine                                                     |
| `SecretName` (variable)| `FanucCredentials`          | Name of the secret holding the FOCAS3 login (passed to the device property `SecretName`) |

The secret `FanucCredentials` is defined in the project's **Secrets Database** (type `UserPassword`,
default user `CncguideAdmin` for the CNC GUIDE 2 simulator). The password is stored encrypted (Ansible
Vault) in the project file and is written to `Secrets/` of the published target.

When the gateway starts, the log confirms the protocol selection and the successful login, e.g.:

```text
Fanuc protocol Focas3 selected by address 'focas3://127.0.0.1:8193'.
Connected to Fanuc: Address=focas3://127.0.0.1:8193; Control='Fanuc 501i-A'; Series='T series'.
```

## The FANUC Device (`FanucControl500i_v1`)

| Node / Pool                                | Type       | Address                  | Purpose                                         |
| :----------------------------------------- | :--------- | :----------------------- | :---------------------------------------------- |
| `Available`                                | Int        | `Global.System.Int32:1`  | Connection / availability of the controller     |
| `SignOfLife`                               | Int        | `Global.System.Int32:2`  | Heartbeat counter                               |
| `Controller/OperationMode`                 | Int        | `Nc1.Dynamic.Float64:8`  | Manual / Automatic / MDI …                      |
| `Controller/RunningState`                  | Int        | `Nc1.Dynamic.Float64:5`  | Program execution state                         |
| `Controller/AlarmState`                    | Int        | `Nc1.Dynamic.Float64:6`  | Alarm active                                    |
| `Controller/EmergencyState`                | Int        | `Nc1.Dynamic.Float64:7`  | Emergency stop state                            |
| `Controller/PowerOnTime` / `OperationTime` / `CuttingTime` | Double (min) | `Nc1.Timer.Float64:0..2` | Controller timers                  |
| `Controller/PartCounter`                   | Int        | `Nc1.Parts.Int32:22`     | Workpiece count                                 |
| `Controller/FeedrateOverride`              | Double (%) | *(processor)*            | Feed-rate override in percent                   |
| `Controller/SpindleOverride`               | Double (%) | *(processor)*            | Spindle override in percent                     |
| `Controller/NCPath1/MainProgram` / `CurrentProgram` | Str | `Nc1.Dynamic.Float64:0/1` | Active main / current NC program             |
| `Controller/NCPath1/MainProgramHeader`     | Str        | `Nc1.Program.String:10`  | Header (comment) of the main program            |
| `Controller/NCPath1/CurrentNcBlock` / `CurrentSequenceNr` | Str | `Nc1.Program.String:2` / `Nc1.Dynamic.Float64:2` | Current NC block / sequence number |
| `Controller/NCPath1/CurrentFeed` / `ProgrammedFeed` | Double (mm/min) | `Nc1.Dynamic.Float64:4` / `Nc1.Modal.Float64:4°5` | Actual / programmed feed |
| `Controller/NCPath1/CurrentSpindleSpeed` / `ProgrammedSpindleSpeed` | Double (1/min) | `Nc1.Spindle.Float64:1°1` / `Nc1.Dynamic.Float64:3` | Actual / programmed spindle speed |
| `Controller/NCPath1/CurrentToolId`         | Str        | `Nc1.Modal.Float64:4°19` | Active tool (T code)                            |
| `Controller/NCPath1/MachinePositions` / `AbsolutePositions` / `RelativePositions` / `DistanceToGo` | Double[] (mm) | `Nc1.Dynamic.Float64:100/200/300/400` | Axis positions |
| `MachineAlarming`                          | Events     | `Nc1.NcAlarmEvent:0`     | NC system alarm/event pool (history 720 h)      |

The `Controller/Private` group holds the raw PMC override bytes (`Pmc1.Pmc_G.Uint8:12` / `:13`) and is
hidden from OPC-UA (`opc-ua:Ignore`). A small **DataProcessors** network converts them into percentages:

| Processor                   | Type                      | Role                                                         |
| :-------------------------- | :------------------------ | :----------------------------------------------------------- |
| `FeedrateOverrideProcessor` | `SituationProcessingNode` | `FeedrateOverrideRaw` → `FeedrateOverride` (`255 - Input`)   |
| `SpindleOverrideProcessor`  | `SituationProcessingNode` | `SpindleOverrideRaw` → `SpindleOverride` (`255 - Input`)     |

Processors and data nodes are wired by **port matching** (`PortMatchId`, `PortMatchingRule`) rather than
explicit links. The template also contains an (empty) `ProgramManagement` skill as an extension point for
program-related commands.

## Prerequisites

- HumanOS IoT Runtime ≥ 2.12 with the **FANUC Control plugin** (`HumanOS.UHAL.FanucControl`) including
  FOCAS3 support, and the **OPC-UA Server plugin** (`HumanOS.PeSeL.OPCUAServer`)
- `FANUC CNC GUIDE 2` simulator (500i-A) listening on `127.0.0.1:8193` — or a physical FANUC 500i-A
  controller with FOCAS3 enabled and a user account for the gateway
- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) or another
  OPC-UA client to inspect the published data (`opc.tcp://localhost:4840`)

## Configuration

- **Target controller**: in the `default` target, edit `Devices/FanucControl.json` (and the device entry in
  `FanucControl500i.h2proj`) and set `Address` to `focas3://<ip>:<port>` of your controller.
- **Credentials**: in the IoT Designer, edit the secret `FanucCredentials` in the Secrets Database and enter
  the username/password of the FOCAS3 user configured on the controller. To use a different secret, change
  the device variable `SecretName`.
- **Vault key**: the project uses `SecurityMode = StoreKeyInProject`. For production, consider supplying the
  vault key via the `HumanOSVaultKey` environment variable instead of storing it in the project.
- **Polled data**: adjust the task processors / memory mappings in
  `default/HumanOS.UHAL.FanucControl/settings.json` to control which FANUC memory areas are read and at
  what priority.
- **OPC-UA**: change port, server URI or root browse name in
  `default/HumanOS.PeSeL.OPCUAServer/settings.json`.
- **Deployment**: publishing the `default` target creates `Build/default/`, including
  `RegisterService.ps1` / `UnRegisterService.ps1` to install the gateway as a Windows service.

## See Also

- [FANUC Data Aggregation](../FANUC.DataAggregation/Readme.md) — classic FOCAS2 connection (`NCGUIDE!`)
- [FANUC MCP Server](../FanucMcpServer/Readme.md) — FANUC data exposed to AI agents via MCP
- [FANUC Control Driver Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FanucControl/)
- [HumanOS Runtime Reference Manual](https://doc.cybertech.swiss/runtime/intro)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
