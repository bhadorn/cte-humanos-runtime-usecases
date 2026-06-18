---
id: usecase-tcpip-demo
title: "Use Case: TCP/IP Machine Data Acquisition via CSV Stream"
subject: "Generic TCP/IP Connectivity with CSV Payload Parsing and OEE Derivation"
keywords: [HumanOS, TCP/IP, TcpClientControl, CSV, payload processor, OEE, machine data acquisition, CNC, industrial automation, OPC-UA]
---

# TCP/IP Demo

Demonstrates how to connect any machine that exposes data over a raw TCP/IP socket to HumanOS using the **TcpClientControl** connector. The machine transmits semicolon-delimited CSV frames every few seconds; a custom C# payload processor (`ctePayloadProcessor.cs`) parses the stream and writes the values into the HumanOS node space. A lightweight .NET 10 **test server** is included to simulate the machine without physical hardware.

## Architecture

```text
TCPServer.exe  (test simulator, port 7872)
        │  raw TCP/IP stream: CSV frames every 5 s
        │  TcpClientControl connector  (address: localhost:7872)
        ▼
ctePayloadProcessor  ←  parses semicolon-delimited CSV
        │
        ▼
HumanOS NodeSpace (data nodes)
   ├── MachineStateProcessor   → OEE machine state code + name
   ├── ErrorProcessor          → error severity classification
   └── PlatformDataAggregator  → OEE entity stream (platform)
        │
        ▼
OPC-UA Server (port 4840)
```

## Prerequisites

- [HumanOS IoT Designer](https://data.cybertech.swiss/public.php/dav/files/LgBzNjG2wtRPXFM/?accept=zip), includes the trial runtime for local testing
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build and run the test server (`TestServerClient/TCPIPClientServer/`)
- Optional: [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) to browse the OPC-UA address space

## Test Server and Client

The `TestServerClient/` folder contains two .NET 10 console applications:

| Project     | Role                                                                                                              |
| :---------- | :---------------------------------------------------------------------------------------------------------------- |
| `TCPServer` | Listens on TCP port 7872, broadcasts randomised CSV frames every 5 seconds to all connected clients               |
| `TCPClient` | Connects to `127.0.0.1:7872`, prints received frames to the console, and allows sending manual text messages back |

Start `TCPServer` first, then open the HumanOS project (`Maschine/`). The gateway connects automatically as a TCP client. `TCPClient` is optional and useful for inspecting the raw stream.

## CSV Payload Format

The server sends a header line followed by data records, both semicolon-delimited:

```text
"Name"; "State"; "ErrCode"; "ProgramName"; "StartDateTime"; "ProductionTime"; "OperationTimer"; "PartCounter";
"Maschine 97590"; 1; 1086; ""; "--.--.---- --:--:--"; "--:--:--"; "0:22"; 2;
```

The header is emitted on the first frame and repeated every 10 cycles (~50 s). `ctePayloadProcessor.cs` detects two-line frames, uses the second line as the data record, and maps each field to the corresponding data node by position.

Time values are normalised to **total minutes**:

| Field            | Raw format | Stored as     |
| :--------------- | :--------- | :------------ |
| `ProductionTime` | `hh:mm:ss` | total minutes |
| `OperationTimer` | `hhhh:mm`  | total minutes |

## Data Nodes

| Node name             | Type    | Source     | Description                               |
| :-------------------- | :------ | :--------- | :---------------------------------------- |
| `MachineId`           | String  | CSV field  | Machine name / identifier                 |
| `MachineState`        | Int32   | CSV field  | Raw machine state code (1-4)              |
| `ErrorId`             | Int32   | CSV field  | Active error code                         |
| `OEEProductName`      | String  | CSV field  | Active NC program name                    |
| `StartDateTime`       | String  | CSV field  | Production start timestamp                |
| `ProductionTime`      | Double  | CSV field  | Cycle time in minutes                     |
| `OperationTimer`      | Double  | CSV field  | Operation timer in minutes                |
| `PartCounter`         | Int32   | CSV field  | Total part counter                        |
| `MachineStateName`    | String  | Derived    | Human-readable machine state (German)     |
| `OEEMachineState`     | Int32   | Derived    | Normalised OEE state code                 |
| `OEEMachineStateName` | String  | Derived    | Normalised OEE state name (English)       |
| `ErrorSeverity`       | String  | Derived    | Error severity classification             |
| `Available`           | Boolean | Stream     | TCP connection alive                      |
| `SignOfLife`          | Boolean | Stream     | Gateway heartbeat                         |
| `PlatformData`        | Entity  | Aggregated | OEE entity stream for platform forwarding |

## Processing Network

### `MachineStateProcessor` (SituationProcessingNode)

Derives a standardised OEE machine state code from the raw `MachineState` value:

| Condition           | OEE code | OEE state name     |
| :------------------ | :------: | :----------------- |
| `!Available`        |   900    | Power Off          |
| `MachineState == 1` |   300    | Production Stopped |
| `MachineState == 2` |   200    | Production         |
| `MachineState == 3` |    1     | Disturbance        |
| `MachineState == 4` |   100    | Setup              |
| (fallback)          |    0     | Not defined state  |

### `ErrorProcessor` (SituationProcessingNode)

Classifies `ErrorId` into a severity string based on numeric ranges:

| Condition        | Severity         |
| :--------------- | :--------------- |
| `ErrorId < 1000` | No error pending |
| `ErrorId < 2000` | Error            |
| `ErrorId < 3000` | Warning          |
| (fallback)       | Information      |

### `PlatformDataAggregator` (DataAggregator)

Aggregates OEE-relevant data nodes (state code, product name, timers, part counter) into a `TGenericEntity` stream for forwarding to the HumanOS Platform via MQTT, REST, RabbitMQ, InfluxDB, or Azure IoT Hub. (Disabled for this project in the device settings.)

## Device Template (`cteCsv.json`)

The reusable device template in `Maschine/DeviceTemplates/cteCsv.json` defines the same node structure as the concrete device instance. It can be used as a starting point for any machine that exposes data in the same CSV protocol.

## Configuration

The HumanOS project is pre-configured to connect to `localhost:7872`. To point it at a real machine:

1. Open `Maschine/default/Devices/Machine.json`.
2. Change the `Address` field to the machine's IP and port, e.g. `192.168.1.50:7872`.

The OPC-UA server is pre-configured on the standard port 4840 (`Maschine/default/HumanOS.PeSeL.OPCUAServer/settings.json`).

## See Also

- [TcpClientControl Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.TcpClientControl/)
- [HumanOS OEE Model](https://doc.cybertech.swiss/runtime/Models/HumanOS.OEE/)
- [OPC-UA Server Plugin Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [C# Scripting Guide](https://doc.cybertech.swiss/runtime/Development/)
- [HumanOS IoT Runtime Reference](https://doc.cybertech.swiss/runtime/intro)
