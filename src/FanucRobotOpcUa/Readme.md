---
id: usecase-fanuc-robot-opcua
title: "Use Case: FANUC Robot OPC-UA Bridge"
subject: "FANUC R-30iB Plus — OPC-UA to OPC-UA Bridge via HumanOS"
keywords: [HumanOS, FANUC, OPC-UA, robot, R-30iB, NanoUaServer, bridge, motion, robotics, industrial automation]
---

# FANUC Robot OPC-UA Bridge

Connects a **FANUC R-30iB Plus** robot controller to HumanOS IoT Runtime via its built-in OPC-UA interface (FANUC NanoUaServer). All robot telemetry — axis positions, torques, operational state, alarms, and Modbus arrays — is read from the controller and re-published through HumanOS's own OPC-UA server, making it accessible to any standard OPC-UA client without custom programming.

## Architecture

```text
FANUC R-30iB Plus
  OPC-UA NanoUaServer  (opc.tcp://192.168.1.100:4880)
          │  OpcUaControl connector  (500 ms polling)
          ▼
  HumanOS Node Space
  ├── RobotInformation   (state, mode, speed, position, torque, alarm)
  ├── MotionDeviceSystem (axis positions, safety state, program info)
  └── ModbusArrays       (DI/DO, registers, command strings)
          │  HumanOS.PeSeL.OPCUAServer plugin
          ▼
  HumanOS OPC-UA Server  (opc.tcp://localhost:4840)
          │
          ▼
  OPC-UA Clients (UAExpert, SCADA, MES, …)
```

## Prerequisites

- HumanOS IoT Runtime ≥ 2.11
- FANUC R-30iB Plus controller with OPC-UA option enabled, reachable from the gateway (default: `192.168.1.100`, port `4880`)
- Optional: [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) to browse the exposed OPC-UA address space

## Key Components

### Device Template — `FanucRobotOpcUa_v1.json`

Declares all data nodes grouped into four namespaces:

| Group | Nodes | Access |
| :---- | :---- | :----- |
| **RobotInformation** | Model, SerialNumber, Version, ServoState, OperationState, ModeState, ProgramSpeed, Uptime, Position[], Torque[], Alarm | Read |
| **MotionDeviceSystem** | SpeedOverride, J1–J6 ActualPosition, OperationalMode, EmergencyStop, ProtectiveStop, TaskProgramName, TaskProgramLoaded | Read / Write |
| **ModbusArrays** | DiscreteInput[], Coils[], InputRegisters[], HoldingRegisters[], Command[] | Read / Read-Write / Write |
| **Root** | Available, SignOfLife | Read |

### OPC-UA Driver — `HumanOS.UHAL.OpcUaControl`

Connects to the FANUC NanoUaServer and polls all mapped nodes every **500 ms**. Certificates are auto-generated; all peer certificates are accepted (development profile). Reconnects automatically after a 10-second delay on connection loss.

### OPC-UA Server — `HumanOS.PeSeL.OPCUAServer`

Exposes the HumanOS node space on **port 4840** under the root browse name `HumanOS`. All nodes are addressable via `ns=1;s=<path>`.

## Data Model

### RobotInformation

| Node | Type | Description |
| :--- | :--- | :---------- |
| `ServoState` | Byte | `0` = Servo OFF, `1` = Servo ON |
| `OperationState` | UInt16 | Bitmask — E-STOP, Fault, Program running, Paused, Held, TP enabled, Battery alarm, Busy |
| `ModeState` | String | `AUTOMATIC`, `MANUAL` (T1), `MANUAL_DATA_INPUT` (T2) |
| `ProgramSpeed` | Byte | Override speed in % |
| `Position` | Single[] | Current joint/Cartesian positions (up to 6 axes), unit: deg / mm |
| `Torque` | Single[] | Q-phase current per axis (up to 6 axes), unit: A |
| `Alarm` | String | Active alarm string, e.g. `"SRVO-001 Operator panel E-stop"` |

### MotionDeviceSystem

Follows the **OPC-UA for Robotics** information model:

- `MotionDevice_1/SpeedOverride` — programmatic speed override (read/write, %)
- `MotionDevice_1/J1`–`J6_ActualPosition` — individual axis positions in °
- `SafetyState_1/OperationalMode` — `0` OTHER · `1` T1 · `2` T2 · `3` AUTO_LOCAL · `4` AUTO_REMOTE
- `SafetyState_1/EmergencyStop` / `ProtectiveStop` — Boolean safety signals
- `Controller_1/TaskProgramName` / `TaskProgramLoaded` — active program info

### ModbusArrays

Direct access to the controller's Modbus memory map:

| Node | Direction | Content |
| :--- | :-------- | :------ |
| `DiscreteInput` | Read | DI, RI, UI arrays |
| `Coils` | Read / Write | DO, RO, F arrays |
| `InputRegisters` | Read | GI, GO, AI, AO arrays |
| `HoldingRegisters` | Read / Write | R[], PR[], alarm history, program status |
| `Command` | Write | 128-byte command strings |

## Configuration

Adjust the robot IP address in `default/Devices/FANUC Robot.json`:

```json
"Address": "opc.tcp://192.168.1.100:4880/FANUC/NanoUaServer"
```

The OPC-UA server port (default `4840`) is set in `default/HumanOS.PeSeL.OPCUAServer/settings.json`.

## See Also

- [OPC-UA Control Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.OpcUaControl/)
- [OPC-UA Server Plugin Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [HumanOS IoT Runtime Reference](https://doc.cybertech.swiss/runtime/intro)
- [OPC-UA for Robotics Specification](https://opcfoundation.org/developer-tools/documents/view/222)
- [UAExpert OPC-UA Client](https://www.unified-automation.com/products/development-tools/uaexpert.html)
