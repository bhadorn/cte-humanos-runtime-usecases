---
id: smart-factory-simulator
title: "SmartFactory Simulator"
subject: "HumanOS IoT Runtime: SmartFactory Simulation"
keywords: [HumanOS, IoT, simulation, robot, AGV, OEE, OPC-UA, MQTT, vHub, workflow]
---

# SmartFactory Simulator

A complete HumanOS IoT Runtime use case that simulates a small smart factory floor with autonomous robots, test machines, and a storage unit: all driven by the HumanOS workflow engine, no physical hardware required.

## Overview

The simulator models a production cell in which **transport robots** move parts between a **storage unit** and **test machines**. Each simulated device exposes realistic telemetry (position, battery level, OEE machine states, part counters) through OPC-UA and can be controlled via a REST API. Waypoints for robot routes are fetched from the [vHub](https://api.vhub.ch) cloud dataset service.

```text
  ┌──────────┐     ┌──────────────┐     ┌──────────────┐
  │ Storage1 │<───>│   Robot 1/2  │<───>│ TestMachine  │
  │          │     │ (AGV, 2× )   │     │  1 / 2       │
  └──────────┘     └──────┬───────┘     └──────────────┘
                          │ fetch waypoints
                    ┌─────▼──────┐
                    │    vHub    │
                    │ (cloud API)│
                    └────────────┘
```

The gateway runtime is named **smartfactory** and hosts:

| Service plug-in                     | Role                                                                 |
| ----------------------------------- | -------------------------------------------------------------------- |
| `HumanOS.UHAL.FileReader`           | Simulation driver: persists and restores device state via JSON files |
| `HumanOS.UHAL.WebControl`           | HTTP client: fetches robot waypoints from vHub REST API              |
| `HumanOS.UHAL.DeviceDetectors`      | Detects and registers all configured devices                         |
| `HumanOS.PeSeL.WebService`          | REST API on port **80**: external command interface                  |
| `HumanOS.PeSeL.OPCUAServer`         | OPC-UA server on port **4840**: industrial data access               |
| `HumanOS.PeSeL.MQTTClient`          | MQTT publisher (disabled by default): data-lake streaming            |
| `HumanOS.PeSeL.NodeSpaceDataLogger` | Persistent data logging                                              |

## Simulated Devices

### Robot (RobotSimulator_v1)

Models an **Automated Guided Vehicle (AGV)**. Two instances are configured: `Robot1` and `Robot2`.

**Data groups and nodes:**

| Group        | Node                  | Type         | Description                        |
| ------------ | --------------------- | ------------ | ---------------------------------- |
| *(root)*     | `OEEMachineState`     | Int32        | Numeric OEE state code             |
| *(root)*     | `OEEMachineStateName` | String       | Human-readable OEE state           |
| *(root)*     | `RoboterData`         | Stream       | Aggregated telemetry entity        |
| *(root)*     | `TaughtWayPoints`     | String       | JSON of all taught routes          |
| *(root)*     | `WorkplaceStateData`  | Stream       | Aggregated workplace state         |
| `Battery`    | `BatteryCapacity`     | Double (Ah)  | Remaining battery capacity         |
| `Battery`    | `BatteryLevel`        | Double (%)   | Battery percentage                 |
| `Battery`    | `PowerConsumption`    | Double (A)   | Instantaneous current draw         |
| `Battery`    | `IsPowerOn`           | Boolean      | Power state                        |
| `Battery`    | `IsCharging`          | Boolean      | Charging state                     |
| `Controller` | `PositionX / Y`       | Double (m)   | Current 2D position                |
| `Controller` | `HorizontalAngle`     | Double (°)   | Heading angle                      |
| `Controller` | `RunningState`        | Int32        | 0 = Idle, 1 = Running, 2 = Stopped |
| `Controller` | `FeedrateOverride`    | Double (%)   | Speed override percentage          |
| `Controller` | `SetSpeed`            | Double (m/s) | Target travel speed                |
| `Controller` | `RunningMeters`       | Double (m)   | Total odometer                     |
| `Controller` | `AlarmState`          | Int32        | 0 = OK, non-zero = fault           |
| `Controller` | `CurrentProgram`      | String       | Active waypoint list (JSON)        |
| `Controller` | `SetTravelDistance`   | Double (m)   | Planned route distance             |
| `Controller` | `SetTravelTime`       | Double (min) | Planned route time                 |
| `Controller` | `CurrentTravelTime`   | Double (min) | Elapsed travel time                |
| `Tray`       | `CurrentJobId`        | String       | Job currently carried              |
| `Tray`       | `NumberOfParts`       | Int32        | Parts on the tray                  |

**Commands:**

| Command          | Arguments                   | Description                                       |
| ---------------- | --------------------------- | ------------------------------------------------- |
| `Start`          | `Speed` (m/s), `JobId`      | Begin transport along the route for the given job |
| `Stop`           |                             | Halt the robot                                    |
| `Teach`          | `Start`, `End`, `WayPoints` | Store a new route between two locations           |
| `ChangeFeedrate` | `Feedrate` (%)              | Override the travel speed percentage              |

**OEE machine states** (computed by `SituationProcessingNode`):

| Code | State name           | Condition            |
| ---- | -------------------- | -------------------- |
| 900  | Power Off            | `IsPowerOn == false` |
| 1    | Disturbance          | `AlarmState != 0`    |
| 100  | Charging             | `IsCharging == true` |
| 230  | Transporting         | `RunningState == 1`  |
| 300  | Transporting Stopped | `RunningState == 2`  |
| 340  | Stopped: Missing Job | *(default)*          |

### TestMachine (TestMachineSimulator_v1)

Models a **processing machine** with configurable cycle time. Two instances are configured: `TestMachine1` and `TestMachine2`.

**Key nodes:**

| Node                          | Type           | Description                                       |
| ----------------------------- | -------------- | ------------------------------------------------- |
| `OEEMachineState / Name`      | Int32 / String | OEE state code and label                          |
| `IsPowerOn`                   | Boolean        | Power state                                       |
| `TrayIn`                      | String         | JSON job placed by the robot at the machine input |
| `TrayOut`                     | String         | JSON job ready for robot collection at output     |
| `Controller.RunningState`     | Int32          | 0 = Idle, 1 = Processing                          |
| `Controller.AlarmState`       | Int32          | 0 = OK, non-zero = fault                          |
| `Controller.CurrentJobId`     | String         | Currently active job                              |
| `Controller.PartsToProcess`   | Int32          | Remaining parts in the current job                |
| `Controller.PartsProcessed`   | Int32          | Parts finished in the current job                 |
| `Controller.TotalPartCounter` | Int32          | Lifetime part total                               |

**Commands:** `LoadJob(JobId, NumberOfParts)`, `UnloadJob() → (JobId, NumberOfParts)`

**Variable:** `CycleTime` (default `120` seconds per part): set per device instance.

**OEE machine states:**

| Code | State name           | Condition            |
| ---- | -------------------- | -------------------- |
| 900  | Power Off            | `IsPowerOn == false` |
| 1    | Disturbance          | `AlarmState != 0`    |
| 200  | Testing              | `RunningState == 1`  |
| 300  | Testing Stopped      | `RunningState == 2`  |
| 310  | Stopped: Missing Job | *(default)*          |

### Storage (Storage_v1)

Models a **part storage station** (`Storage1`).

| Node          | Type        | Description                                               |
| ------------- | ----------- | --------------------------------------------------------- |
| `Temperature` | Double (°C) | Ambient temperature (simulated by `TemperatureProcessor`) |
| `JobRegistry` | String      | JSON registry of stored jobs                              |
| `StoredParts` | Int32       | Total parts currently held                                |

**Commands:** `LoadJob(JobId, NumberOfParts)`, `UnloadJob(JobId) → NumberOfParts`

Temperature is continuously generated by a `CSharpScriptProcessingNode` running `StorageSimulator.TemperatureProcessing.cs`.

### vHub (vHub_v1)

Cloud-based **route registry** at `https://api.vhub.ch`. The robot controller queries it periodically and whenever `CurrentJobId` changes to refresh waypoints.

The `ReadWayPoints` command fetches dataset `93b71904-4967-48d7-8a0f-4e56711c6c22` and filters by `RouteId` (the job ID / path name), returning an ordered JSON array of `{PosX, PosY}` waypoint objects.

## Workflow Engine

Each robot and test machine is controlled by a **two-step workflow**:

```text
OnObjectAppeared(RobotWorkflow)
        │
        ▼
  RobotInit          ← resolves and registers device, Controller,
        │               Battery, Tray, vHub into the workflow context
        ▼
  RobotController    ← continuous async loop:
                        • fetch waypoints from vHub on job change
                        • simulate movement along the waypoint path
                        • update position, odometer, battery level
                        • consume power proportional to speed × time
```

The `RobotController` loop steps through waypoints using vector interpolation. It halts if `RunningState` drops to 0, `BatteryLevel` falls below 1%, or a cancellation is requested.

The `TestMachineController` loop:

1. Polls `TrayIn` for a new job JSON (`{JobId, NumberOfParts}`)
2. Accepts the job, processes parts one-by-one with a `CycleTime` delay
3. Writes the finished job JSON to `TrayOut`, clearing `CurrentJobId`

## REST API (port 80)

Exposed by `HumanOS.PeSeL.WebService` at `http://<gateway>/CommandInterface/`.

### Robot

| Method | Path                    | Body                                                    | Description                  |
| ------ | ----------------------- | ------------------------------------------------------- | ---------------------------- |
| POST   | `/Robot/Start`          | `{"RobotId":"Robot1","JobId":"…","Feedrate":1.5}`       | Start a robot transport run  |
| POST   | `/Robot/Stop`           | `{"RobotId":"Robot1"}`                                  | Stop a running robot         |
| POST   | `/Robot/Teach`          | `{"RobotId":"…","Start":"…","End":"…","WayPoints":"…"}` | Store a new route            |
| POST   | `/Robot/ChangeFeedrate` | `{"RobotId":"…","Feedrate":…}`                          | Change the feedrate override |

### Storage

| Method | Path              | Body                                                     | Description                 |
| ------ | ----------------- | -------------------------------------------------------- | --------------------------- |
| POST   | `/Storage/Load`   | `{"StorageId":"Storage1","JobId":"…","NumberOfParts":…}` | Load a job into storage     |
| POST   | `/Storage/Unload` | `{"StorageId":"Storage1","JobId":"…"}`                   | Retrieve a job from storage |

## OPC-UA (port 4840)

All devices and their nodes are published under the browse path `HumanOS/<device-name>/…`. Use [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) or any OPC-UA client to connect to `opc.tcp://<gateway>:4840`.

## MQTT Data Lake (optional)

The `DataPublishLogger` publisher (disabled by default in `HumanOS.PeSeL.MQTTClient/settings.json`) streams robot telemetry to `datalake/stream` on a local MQTT broker. To enable it, set `"Disabled": false` and configure the broker address and credentials.

The published payload is formatted by `TDataLakePayload.cs` and includes: `Id`, `Name`, `DeviceId`, `TimeStamp`, `Value`, `DataType`, `State`. Only nodes with the `EnableRoboterData=true` property are included.

## Directory Structure

```text
SmartFactorySimulator/
├── SmartFactory Simulator.h2proj      HumanOS IoT Designer project file
├── globalids.json                     Global IDs registry
├── DeviceTemplates/                   Reusable device type definitions
│   ├── RobotSimulator_v1.json
│   ├── TestMachineSimulator_v1.json
│   ├── Storage_v1.json
│   └── vHub_v1.json
├── Examples/                          Initial state snapshots for device instances
│   ├── Robot1.json / Robot2.json (in smartfactory/Devices)
│   ├── Storage1.json
│   └── TestMachine1.json / TestMachine2.json
└── smartfactory/                      Runtime configuration for the "smartfactory" gateway
    ├── Devices/                       Device instance files (Robot1, Robot2, TestMachine1,
    │                                  TestMachine2, Storage1, vHub)
    ├── DataModel/
    │   ├── Objects/Simulator.Schema.json
    │   ├── Rules/Rules.Schema.json
    │   └── Schema/                    Workflow schemas (RobotWorkflow, TestMachineWorkflow)
    ├── Scripts/Workflows/
    │   ├── RobotInit.cs               Workflow init: resolves device sub-nodes into context
    │   ├── RobotController.cs         Async movement simulation loop
    │   ├── TestMachineInit.cs         Workflow init for test machines
    │   └── TestMachineController.cs   Part processing loop
    ├── HumanOS.UHAL.FileReader/       FileReader driver: simulator scripts per device type
    │   ├── RobotSimulator.Start/Stop/Teach/ChangeFeedrate.cs
    │   ├── StorageSimulator.LoadJob/UnloadJob/TemperatureProcessing.cs
    │   └── TestMachineSimulator.LoadJob/UnloadJob.cs
    ├── HumanOS.UHAL.WebControl/       HTTP client: vHub waypoint fetch script
    │   └── ReadWayPoints.cs
    ├── HumanOS.UHAL.DeviceDetectors/  settings.json
    ├── HumanOS.PeSeL.WebService/      REST API endpoint scripts + settings.json (port 80)
    │   ├── Robot.Start/Stop/Teach/ChangeFeedrate.cs
    │   └── Storage.Load/Unload.cs
    ├── HumanOS.PeSeL.OPCUAServer/     settings.json (port 4840)
    ├── HumanOS.PeSeL.MQTTClient/      settings.json + TDataLakePayload.cs
    └── HumanOS.PeSeL.NodeSpaceDataLogger/ settings.json + TDataLakePayload.cs
```

## Prerequisites

- [HumanOS IoT Designer](https://data.cybertech.swiss/public.php/dav/files/LgBzNjG2wtRPXFM/?accept=zip) with the trial runtime
- A **vHub API token**: set the `AuthorizationParameter` in `smartfactory/Devices/vHub.json` to your token
- (Optional) An MQTT broker for data-lake streaming
- (Optional) [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) to inspect OPC-UA output

## Getting Started

1. Open `SmartFactory Simulator.h2proj` in HumanOS IoT Designer.
2. Set your vHub token in the `vHub` device instance (`AuthorizationParameter`).
3. Deploy and start the **smartfactory** gateway (or use the trial runtime directly from the Designer).
4. The workflow engine will automatically initialise each robot and test machine after a 10-second startup delay.
5. Use the REST API or OPC-UA to interact with the simulation:
   - Send `POST /Robot/Start` with a `JobId` and `Feedrate` to dispatch a robot.
   - Monitor `OEEMachineState`, `PositionX/Y`, and `BatteryLevel` via OPC-UA.

## References

- [HumanOS Runtime Reference Manual](https://doc.cybertech.swiss/runtime/intro)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
- [vHub API](https://api.vhub.ch)
