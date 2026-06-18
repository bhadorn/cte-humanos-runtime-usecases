---
id: usecase-traub-opcua
title: "Use Case: Traub CNC OPC-UA Integration"
subject: "Traub CNC: OPC-UA Bridge with OEE State Derivation via HumanOS"
keywords: [HumanOS, Traub, CNC, OPC-UA, OEE, MDE, machine state, simulator, ctProduction, industrial automation]
---

# Traub CNC OPC-UA Integration

Connects a **Traub CNC turning machine** to HumanOS IoT Runtime via its built-in OPC-UA interface. Machine signals (cycle times, spindle speed, feed rate, part counters, alarm state, program info) are read from the controller, processed into OEE machine states, and forwarded to the CyberTech platform, MQTT, Azure IoT Hub, and REST.

A self-contained **TraubSimulator** project is included: it acts as a software replica of the real Traub OPC-UA server so the integration can be developed and tested without physical hardware.

## Architecture

```text
control.json  (editable signal values)
      |  HumanOS.UHAL.FileReader
      v
TraubSimulator  (HumanOS project)
  HumanOS.PeSeL.OPCUAServer  (opc.tcp://localhost:4880)
      |  OpcUaControl connector
      v
cte  (HumanOS project)
  Device: cte_Traub_ctProduction  (OPC-UA client, ns=2)
  ├── Controller group   (AlarmState, RunningState, OperationMode,
  │                       CycleTime, OperationTime, PowerOnTime,
  │                       FeedrateOverride, SpindleOverride, PartCounter, …)
  └── NCPath1 group      (MainProgram, MainProgramHeader, CurrentToolId,
                          CurrentFeed, CurrentSpindleSpeed, …)
      |  Processing Network (PortMatchingRule wires ports automatically)
  ├── MachineStateProcessor       →  OEEMachineState / OEEMachineStateName
  ├── MainProgramHeaderProcessor  →  OEEProductName / OEEProductionStep
  └── PlatformDataAggregator      →  PlatformData (TGenericEntity stream)
      |
      v
  OPC-UA Server (port 4880), MQTT, REST, Azure IoT Hub, InfluxDB, RabbitMQ
```

## Projects

| Project           | Role                                                                                                         |
| :---------------- | :----------------------------------------------------------------------------------------------------------- |
| `TraubSimulator/` | Simulates the Traub OPC-UA server; reads signal values from `control.json` and publishes them on port 4880   |
| `cte/`            | Production integration project; connects to the real Traub machine (or the simulator) and processes the data |

## Prerequisites

- HumanOS IoT Runtime (trial included in [HumanOS IoT Designer](https://data.cybertech.swiss/public.php/dav/files/LgBzNjG2wtRPXFM/?accept=zip))
- For simulation: both HumanOS projects deployed on the same gateway; no additional hardware required
- For a real machine: a Traub CNC controller with OPC-UA server enabled, reachable from the gateway on port 4880
- Optional: [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) to inspect the OPC-UA address space

## Key Components

### TraubSimulator: `control.json`

Defines the current simulated signal values. Edit this file to change what the simulator publishes without restarting the project.

```json
{
  "AlarmState": false,
  "AutoCycle": false,
  "OperationMode": 1,
  "RunningState": false,
  "CycleTime": 185,
  "OperationTime": 5321,
  "PowerOnTime": 125310,
  "PartCounter": 2,
  "PartCounterTotal": 33,
  "CurrentToolId": "Tool1",
  ...
}
```

### TraubSimulator: Device Template `TraubControl_v1.json`

Maps each `control.json` field to an OPC-UA node ID using JSONPath addresses (`$.FieldName`). These node IDs match the real Traub controller's OPC-UA address space so the cte project works with both the simulator and the physical machine.

### cte: Device Template `cte_Traub_ctProduction.json`

An OPC-UA client template that reads from the Traub OPC-UA server. Node addresses use template variables:

| Variable              | Value               |
| :-------------------- | :------------------ |
| `OpcUaNameSpace`      | `ns=2`              |
| `NameSpaceTypePrefix` | `s=`                |
| `DeviceName`          | `Traub NCControl0/` |
| `RootDeviceNode`      | `Objects/Devices/`  |

Addresses are composed as `$(OpcUaNameSpace);$(NameSpaceTypePrefix)<NodeId>`, for example `ns=2;s=893` for `CycleTime`.

### cte: Processing Network

Three processors are chained via `PortMatchId` wiring (no manual port connections needed):

#### MachineStateProcessor

A `SituationProcessingNode` that derives OEE machine states from raw signals:

| Condition                                        | `OEEMachineState` | `OEEMachineStateName`     |
| :----------------------------------------------- | :---------------- | :------------------------ |
| `AvailableState <= 0`                            | 900               | Power Off                 |
| `AlarmState == true`                             | 1                 | Disturbance               |
| `OperationMode != 7`                             | 100               | Setup                     |
| `OperationMode == 7` and `RunningState == true`  | 200               | Production                |
| `OperationMode == 7` and `RunningState == false` | 300               | Production Stopped        |
| `OperationMode == 7` (fallthrough)               | 320               | Stopped: Missing Personal |

#### MainProgramHeaderProcessor

A C# script (`TMainProgramHeaderProcessorScript.cs`) that parses the NC main program header and extracts:

- `OEEProductName`: parsed from `;PRODUCT:<name>` comment line
- `OEEProductionStep`: parsed from `;STEP:<step>` comment line

#### PlatformDataAggregator

Combines OEE state, product name, production step, cutting/operation/power-on times, cycle time, tool ID, feed rate override, and part counters into a single `TGenericEntity` stream for the CyberTech platform.

## Data Model

### Signals (from Traub OPC-UA server)

| Signal                   | OPC-UA Node ID | Type    | Unit                             |
| :----------------------- | :------------- | :------ | :------------------------------- |
| `AlarmState`             | 10811          | Boolean |                                  |
| `AutoCycle`              | 1002           | Boolean |                                  |
| `CuttingTime`            | 11             | Double  | min (converted from s)           |
| `CycleTime`              | 893            | Double  | s                                |
| `FeedrateOverride`       | 30503          | Double  | %                                |
| `OperationMode`          | 30505          | Int32   | 0=MDI, 1=Automatic, other=Manual |
| `OperationTime`          | 892            | Double  | min (converted from s)           |
| `PartCounter`            | 10102          | Int32   |                                  |
| `PartCounterTotal`       | 22             | Int32   |                                  |
| `PowerOnTime`            | 889            | Double  | min (converted from s)           |
| `RunningState`           | 10002          | Boolean |                                  |
| `SpindleOverride`        | 20064          | Double  | %                                |
| `CurrentFeed`            | 11301          | Double  | mm/min                           |
| `CurrentNcBlock`         | 15             | String  |                                  |
| `CurrentProgram`         | 10001          | String  |                                  |
| `CurrentSequenceNr`      | 17             | String  |                                  |
| `CurrentSpindleSpeed`    | 11111          | Double  | 1/min                            |
| `CurrentToolId`          | 18             | String  |                                  |
| `MainProgram`            | 19             | String  |                                  |
| `MainProgramHeader`      | 20             | String  | parsed for OEE product/step      |
| `ProgrammedFeed`         | 21             | Double  | mm/min                           |
| `ProgrammedSpindleSpeed` | 11101          | Double  | 1/min                            |

## Configuration

### Simulator endpoint

The TraubSimulator OPC-UA server endpoint is set in `TraubSimulator/default/HumanOS.PeSeL.OPCUAServer/settings.json` (default: port `4880`).

### Real machine connection

To connect to a physical Traub machine instead of the simulator, update the device address in `cte/default/Devices/OPC-UA Test.json`:

```json
"Address": "opc.tcp://<machine-ip>:4880"
```

The OPC-UA namespace and node IDs must match the controller firmware; adjust `OpcUaNameSpace` and node ID values in `cte/DeviceTemplates/cte_Traub_ctProduction.json` if needed.

### Program header parsing

To populate `OEEProductName` and `OEEProductionStep`, add comment lines to the NC main program header:

```text
;PRODUCT:MyPart
;STEP:OP10
```

## See Also

- [OPC-UA Control Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.OpcUaControl/)
- [OPC-UA Server Plugin Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [HumanOS IoT Runtime Reference](https://doc.cybertech.swiss/runtime/intro)
- [UAExpert OPC-UA Client](https://www.unified-automation.com/products/development-tools/uaexpert.html)
