---
id: usecase-dmg-mori-seiki
title: "Use Case: DMG Mori Seiki Machine Data Acquisition"
subject: "CNC Machine Data Acquisition via Native TCP/IP Protocol"
keywords: [HumanOS, DMG Mori Seiki, MDA, TCP, TcpClientControl, CNC, machine data acquisition, OEE, OPC-UA, payload processor]
---

# DMG Mori Seiki Machine Data Acquisition

Shows how to connect an older **DMG Mori Seiki** CNC controller to HumanOS using the **TcpClientControl** connector and the machine's native TCP/IP data stream. The use case acquires real-time machine data (axis positions, spindle state, program execution, part counter) and maps it to the **HumanOS OEE information model** for downstream consumption via OPC-UA.

## Architecture

```text
DMG Mori Seiki CNC (TCP port 7878)
        │
        │  TcpClientControl connector
        ▼
MoriSeikiPayloadProcessor  ←  parses pipe-delimited ASCII stream
        │
        ▼
HumanOS NodeSpace (data nodes)
   ├── MachineStateProcessor  → OEE machine state code + name
   ├── ProductProcessor       → product name / production step
   ├── FeedrateOverrideProcessor  → normalised feedrate override %
   ├── SpindleOverrideProcessor   → normalised spindle override %
   ├── PlatformDataAggregator     → OEE entity stream (platform)
   └── ProcessDataAggregator      → process data stream @ 10 Hz
        │
        ▼
OPC-UA Server (port 4840)
```

## Prerequisites

- HumanOS IoT Runtime ≥ 2.9
- TcpClientControl plugin licensed and installed on the gateway
- Network access from the gateway to the Mori Seiki controller (default: `192.168.0.10`, TCP port `7878`)
- OPC-UA client (e.g. [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html)) for inspecting acquired data

## Native Payload Format

The machine streams newline-separated ASCII frames. Each frame contains one or more pipe-delimited (`|`) key/value pairs in the form:

```text
|<name>|<value>|<name>|<value>|…
```

The `MoriSeikiPayloadProcessor` reads up to 40 000 bytes per cycle, splits on newlines, then on `|`, and maps each name/value pair to the corresponding data node. Boolean values are normalised:

| Raw value     | Mapped to |
| :------------ | :-------- |
| `ON`          | `True`    |
| `OFF`         | `False`   |
| `AVAILABLE`   | `True`    |
| `UNAVAILABLE` | `False`   |

## Device Template: `MoriSeiki.json`

The device template defines the full set of data nodes acquired from the controller:

| Category          | Data nodes                                                                 |
| :---------------- | :------------------------------------------------------------------------- |
| Program execution | `mode1`, `execution1`, `program1`, `block1`, `line1`                       |
| Execution flags   | `optionalstop1`, `dryrun1`, `cutting1`, `reset1`                           |
| Machine status    | `avail`, `coolant`, `power_state`, `operationmode1`, `machineSN`           |
| Part / tool       | `part_count1`, `toolnumber1`                                               |
| Override values   | `jogoverride1`, `rapidoverride1`, `spindleoverride5`, `spindleoverride6`   |
| Axis load (%)     | `Xload`, `Yload`, `Zload`, `Aload`, `Bload`                               |
| Axis position     | `Xact`, `Yact`, `Zact`, `Aact`, `Bact`, `Cact`                           |
| Spindle (C5/C6)   | `C5speed`, `C6speed`, `C5load`, `C6load`, `C5mode`, `C6mode`, `spindlerotating5`, `spindlerotating6` |

## Processing Network

### `MachineStateProcessor` (SituationProcessingNode)

Derives a standardised OEE machine state code and name from the raw controller states:

| Condition                                     | State code | State name                  |
| :-------------------------------------------- | :--------: | :-------------------------- |
| `AvailableState ≤ 0`                          | 900        | Power Off                   |
| `AlarmState ≠ 0`                              | 1          | Disturbance                 |
| `OperationMode ≠ 1`                           | 100        | Setup                       |
| `OperationMode = 1` and `RunningState = 3`    | 200        | Production                  |
| `OperationMode = 1` and `RunningState = 2`    | 300        | Production Stopped          |
| `OperationMode = 1` and `RunningState = 1`    | 310        | Stopped: Missing Material   |
| `OperationMode = 1` (fallback)                | 320        | Stopped: Missing Personal   |

### `ProductProcessor` (CSharpScriptProcessingNode)

Extracts the product name and production step from the active NC program header using `Fanuc_ProductProcessor.cs`.

### Override Processors (`FeedrateOverrideProcessor`, `SpindleOverrideProcessor`)

Convert the raw controller override value to a normalised percentage using the formula `255 − Input`.

### `PlatformDataAggregator` / `ProcessDataAggregator`

Aggregate OEE and process data into entity streams for forwarding to the HumanOS Platform. The `ProcessDataAggregator` runs at 10 Hz (`SamplingRate: 0.1`).

## Processing Flow

1. The TcpClientControl connector opens a persistent TCP connection to the machine on port 7878.
2. The `MoriSeikiPayloadProcessor` reads each incoming byte stream and parses the pipe-delimited payload into individual name/value pairs.
3. Parsed values are written to the corresponding data nodes in the HumanOS NodeSpace.
4. The processing network processors evaluate the node values, derive OEE states, and aggregate the data into platform entity streams.
5. All data nodes and aggregated entities are exposed via the OPC-UA server on port 4840.

## Configuration

The `default` target configuration connects to `192.168.0.10` on TCP port `7878`. Adjust the device address in `default/Devices/MoriSeiki.json` to match the controller's IP address on your network. The OPC-UA server is pre-configured on the standard port 4840.

## See Also

- [TcpClientControl Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.TcpClientControl/)
- [HumanOS OEE Model](https://doc.cybertech.swiss/runtime/Models/HumanOS.OEE/)
- [OPC-UA Server Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [C# Scripting Guide](https://doc.cybertech.swiss/runtime/Development/)
