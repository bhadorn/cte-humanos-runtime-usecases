---
id: usecase-flames-demo
title: "Use Case: FLAMES OPC-UA Companion Spec Demo"
subject: "FLAMES: Machine Tool Automation Interface via HumanOS OPC-UA"
keywords: [HumanOS, FLAMES, OPC-UA, companion specification, state machine, tool management, production, automation, CNC, industrial automation]
---

# FLAMES Demo

Implements the [FLAMES OPC-UA companion specification](https://www.soflex.de/digitalk/flames), a standardised interface for machine tool automation covering production management, tool data exchange and automation state. A JSON file simulator (`Machine.json`) stands in for a real machine, so the demo runs without any physical hardware.

## Architecture

```text
Machine.json  (file-based simulator)
        │  FileReader connector
        ▼
HumanOS Node Space
├── FLAMES/Machine
│   ├── AutomationState      (RemoteMode, OperationState, ServiceRequest)
│   ├── Positions            (Chuck, Chuck2: occupancy + indicator)
│   ├── ProductionManagement
│   │   ├── ProductionProgram.State  [state machine]
│   │   └── ToolUsedEventData        [event]
│   ├── ToolDataManagement   (Tool1, Tool2: ToolLife, Locked, NativeDataset)
│   └── FileSystem           (dynamic OPC-UA file/directory nodes)
└── General                  (Available, SignOfLife)
        │  OPC-UA Server, FLAMES model (flames.xml)
        ▼
HumanOS OPC-UA Server  (opc.tcp://localhost:4840)
        │  namespace: http://cybertech.swiss/cybertech.IIoTgateway.FLAMES/
        ▼
OPC-UA Clients (UAExpert, MES, robot cell controller, ...)
```

## Prerequisites

- [HumanOS IoT Designer](https://data.cybertech.swiss/public.php/dav/files/LgBzNjG2wtRPXFM/?accept=zip), includes the trial runtime for local testing
- No additional hardware required, machine data is simulated via `Machine.json`
- Optional: [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) to browse the FLAMES OPC-UA address space

## Key Features

### FLAMES OPC-UA Companion Specification

Exposes the standardised FLAMES information model on the OPC-UA server. Machine identity, automation state, tool inventory, production programs, and all kind of positions (e.g. workpiece) are all addressable by any FLAMES-compliant OPC-UA client.

### State Machines

Three state machines are implemented as HumanOS C# processing scripts: **ProductionStateMachine** (Initialized -> Running -> Stopped -> Aborted -> Interrupted), **TransportStateMachine**, and **OccupancyReconciliationStateMachine**. Each state transition fires a typed OPC-UA event carrying the full transition context.

### Event Handling

C# scripts construct and publish strongly-typed OPC-UA events on every relevant state change, including `ProductionProgramTransitionEvent` (program identifier, last tool, quality flag, abort reason) and `ToolUsedEvent` (current tool, diameter, width, tolerance values).

### OPC-UA Information Model Mapping (flames.xml)

All device data nodes are mapped to the FLAMES companion model via `opc-ua:flames:Mapping` properties in the device template, referencing nodes from the Modeler-generated `flames.xml` / `cybertech.iiotgateway.flames.xml`. Type definitions stay in sync with the companion spec without manual node configuration.

### Dynamic FileSystem

The FLAMES FileSystem is exposed as a live OPC-UA `FileDirectoryType` tree. Clients can create, delete, and move directories and files over OPC-UA; the `FileSystemCreateDirectory.cs` script handles all operations at runtime.

### Tool Data Management

Tool1 and Tool2 demonstrate the full FLAMES tool lifecycle: unique identifier, tool life with engineering units, locked flag, native dataset (raw byte string), and unload marker. The `SupplyToolDataset` command allows an external system to push updated tool data back to the machine via OPC-UA.

## Data Model

| Group                    | Key Nodes                                                                                                 |
| :----------------------- | :-------------------------------------------------------------------------------------------------------- |
| **AutomationState**      | RemoteMode, OperationState, ServiceRequest, Trigger                                                       |
| **Positions**            | Chuck / Chuck2: Occupied, UniqueId, SetIndicatorMode                                                      |
| **ProductionManagement** | ProductionProgram.State (state machine), Identifier, LastTool, QualityWithinTolerances, ReasonForAbortion |
| **ToolDataManagement**   | Tool1 / Tool2: Locked, ToolLife, NativeDataset, UnloadMarker, Identifier                                  |
| **FileSystem**           | Dynamic `FileDirectoryType` / `FileType` nodes                                                            |

## Configuration

The simulated machine state is defined in `Machine.json` at the project root. Edit any value in the `Flames` object to change what the OPC-UA server exposes:

```json
{
  "Flames": {
    "UniqueId": "unique-id-145",
    "AutomationState": { "RemoteMode": false, "OperationState": 2 },
    "Positions": { "Chuck": { "Occupied": true } }
  }
}
```

The OPC-UA server port (default `4840`) is configured in `default/HumanOS.PeSeL.OPCUAServer/settings.json`.

## See Also

- [FLAMES Specification Overview](https://www.soflex.de/digitalk/flames)
- [FileReader Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FileReader/)
- [OPC-UA Server Plugin Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [HumanOS IoT Runtime Reference](https://doc.cybertech.swiss/runtime/intro)
- [UAExpert OPC-UA Client](https://www.unified-automation.com/products/development-tools/uaexpert.html)
