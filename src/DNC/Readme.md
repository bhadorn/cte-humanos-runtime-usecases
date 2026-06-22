---
id: usecase-dnc-machine-controls
title: "Use Case: DNC for Different Machine Controls"
subject: "Unified DNC File and Tool Data Exchange for FANUC, Heidenhain, Okuma and Sinumerik Controllers"
keywords: [HumanOS, DNC, CNC, FANUC, Heidenhain, Okuma, Sinumerik, NC programs, tool data, file transfer, OPC-UA, FanucControl, HeidenhainControl, OkumaControl, SinumerikControl]
---

# DNC for Different Machine Controls

Shows how to expose **DNC (Direct Numerical Control) file and tool-data exchange** for several CNC controller families through a single, uniform interface. The same set of DNC commands — file streaming, directory listing and tool-record management — is implemented for **FANUC**, **Heidenhain**, **Okuma** and **Siemens Sinumerik** controllers, so that a client application can drive any of them without knowing the underlying control protocol.

Each controller family is described by its own device template that maps the vendor-specific connector onto the shared **HumanOS DNC information model** (`HumanOS.DNC`). The gateway publishes these commands over its OPC-UA server, and a companion **DNC client** application consumes them.

## Architecture

```text
   CNC controllers                       HumanOS IoT Runtime (Gateway)             DNC Client
┌──────────────────┐                  ┌────────────────────────────────┐      ┌────────────────┐
│ FANUC            │── FanucControl ──▶│                                │      │                │
│ Heidenhain       │── Heidenhain  ───▶│   Device template per control  │      │  OPC-UA client │
│ Okuma            │── OkumaControl ──▶│   → HumanOS.DNC model          │◀────▶│  per machine   │
│ Sinumerik 840D   │── Sinumerik   ───▶│   → OPC-UA Server (NCPath1)    │      │  (DncNodeId)   │
└──────────────────┘                  └────────────────────────────────┘      └────────────────┘
```

Regardless of the controller, the DNC commands are published under the same node path
`<Machine>/Controller/NCPath1`, which the client addresses through its configured `DncNodeId`.

## Project Layout

| Folder                                            | Purpose                                                                                   |
| :------------------------------------------------ | :---------------------------------------------------------------------------------------- |
| `HumanOS.DNC.Gateways/`                           | HumanOS IoT Designer project (`.h2proj`) containing the gateway targets and templates     |
| `HumanOS.DNC.Gateways/DeviceTemplates/`           | One DNC device template per controller family (see below)                                 |
| `HumanOS.DNC.Gateways/default/`                   | Template gateway target with all four control plugins and the OPC-UA server               |
| `HumanOS.DNC.Gateways/HermleC41/`                 | Concrete machine instance (Hermle C41, Heidenhain iTNC530) using `HeidenhainDNC_v1`       |
| `HumanOS.DNC.Gateways/Build/`                     | Deployment-ready, published artifacts for each target (incl. service register scripts)    |
| `HumanOS.DNC.Client/`                             | Configuration for the DNC client application that consumes the published DNC commands     |

## Device Templates

All templates expose the same DNC command surface under `Controller/NCPath1` and the common
status nodes `Available`, `SignOfLife` and the operation/running state of the controller. They
differ only in the underlying connector and in the subset of operations a control supports.

| Template              | Connector            | Tool records | Whole-file read/write | Notes                                              |
| :-------------------- | :------------------- | :----------: | :-------------------: | :------------------------------------------------- |
| `FanucDNC_v1.json`    | `FanucControl`       |      ✔       |          ✔            | `OpenFileStream` takes a `FileType` argument (0 = ISO) |
| `HeidenhainDNC_v1.json` | `HeidenhainControl` |      ✔       |          ✔            | Used by the included `HermleC41` machine instance  |
| `OkumaDNC_v1.json`    | `OkumaControl`       |      –       |          –            | File-based DNC via a directory (`Dnc.Path`, default `C:\Temp\Okuma`); streaming only |
| `SinumerikDNC_v1.json`| `SinumerikControl`   |      ✔       |          ✔            | DNC for Siemens 840D controllers                   |

## DNC Command Set (HumanOS.DNC model)

The DNC commands are exposed as OPC-UA method nodes under `<Machine>/Controller/NCPath1`:

**File streaming** (for large NC programs, transferred in chunks):

- `OpenFileStream` — open a file for read/write, returns a `Handle`
- `ReadFileStream` — read up to `MaxBytes`, signals `LastContent` on the final chunk
- `WriteFileStream` — append a content chunk to an open handle
- `GetFileStreamStatus` — query the status of an open stream (JSON)
- `CloseFileStream` — release a handle

**Whole-file operations** (convenience, for smaller files):

- `ReadFile` / `WriteFile` — read or write a complete file by name
- `DeleteFile` — remove a file

**Directory operations**:

- `ReadDirectory` — list a directory
- `MakeDirectory` / `DeleteDirectory` — create or remove a directory
- `GetCurrentDirectory` / `SetCurrentDirectory` — query or change the working directory

**Tool-record management** (where supported by the control):

- `ReadToolRecord` / `WriteToolRecord` — read or update a tool record by `ToolId`
- `CreateToolRecord` / `DeleteToolRecord` — add or remove a tool record

In addition, the templates publish read-only monitoring nodes such as `CurrentProgram`,
`MainProgram`, `CurrentNcBlock`, `CurrentSequenceNr`, `CurrentToolId`, `RunningState` and
`OperationMode`, plus a `MachineAlarming` event pool for controller alarms.

## Processing Flow

1. A client (the included DNC client, or any OPC-UA client) connects to the gateway's OPC-UA
   server and locates the target machine's `Controller/NCPath1` node.
2. The client calls the DNC command methods (e.g. open a stream, read chunks until
   `LastContent`, then close) to download or upload an NC program.
3. HumanOS routes each call through the matching control connector to the physical controller
   and returns the result to the client.
4. Tool records and directory listings are exchanged through the same uniform command set.

## Prerequisites

- HumanOS IoT Runtime ≥ 2.11
- The control plugin(s) for the controllers you want to integrate, licensed and installed on the
  gateway: `FanucControl`, `HeidenhainControl`, `OkumaControl` and/or `SinumerikControl`
- Network access from the gateway to each controller (or a vendor simulator, e.g. the Heidenhain
  iTNC530 simulator used by the `HermleC41` target)
- An OPC-UA client for the DNC commands — either the included [DNC client](./HumanOS.DNC.Client/readme.md)
  or a generic client such as [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html)

## Configuration

- **Add a machine**: in the `HumanOS.DNC.Gateways` project, add a device based on the template
  matching its control family and set its connection address (the `HermleC41` instance uses the
  Heidenhain address `ItncSimDev`). For Okuma, adjust the `Dnc.Path` variable to the directory
  used for file exchange.
- **OPC-UA server**: the gateway publishes on `opc.tcp://localhost:4840/` (see
  `HumanOS.PeSeL.OPCUAServer/settings.json`). The DNC commands appear under the
  `<Machine>/Controller/NCPath1` node.
- **DNC client**: configure each machine in `HumanOS.DNC.Client/appsettings.json` with its
  `opc:ServerAddress` and `opc:DncNodeId` — see the [DNC client readme](./HumanOS.DNC.Client/readme.md).
- **Deployment**: the `Build/` folder contains published, deployment-ready gateway targets,
  including `RegisterService.ps1` / `UnRegisterService.ps1` to install the gateway as a Windows
  service.

## See Also

- [HumanOS DNC Model](https://doc.cybertech.swiss/runtime/Models/HumanOS.DNC/)
- [DNC Client Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.DncClient/)
- [FANUC Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FanucControl/)
- [Heidenhain Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.HeidenhainControl/)
- [Okuma Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.OkumaControl/)
- [Sinumerik Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.SinumerikControl/)
</content>
</invoke>
