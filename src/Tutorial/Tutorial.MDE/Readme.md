---
id: tutorial-opcua-bridge
title: "Tutorial: OPC-UA Bridge"
subject: "Bridging device data across two independent OPC-UA gateway instances in HumanOS"
keywords: [HumanOS, OPC-UA, bridge, gateway, FileReader, multi-gateway, port, no-code]
---

# Tutorial: OPC-UA Bridge MDE

Shows how to forward data from one HumanOS gateway to an OPC-UA server, without any custom scripting.

This is the foundational pattern for multi-hop data distribution, DMZ bridging, or aggregating data from edge gateways into a central gateway.

## Step by Step Guide

A complete walkthrough is available at [OPC-UA Bridge MDE Step by Step Guide](https://doc.cybertech.swiss/runtime/Tutorials/Tutorial1/06_Example.md).

## Architecture

```text
JSON File  (C:\Temp\TestFile.json)
        │
        ▼
Gateway: default  (port 4840)
  FileReader driver  ──► Device: "JSON Simulator"
  OPC-UA Server      ──► publishes nodes on opc.tcp://localhost:4840
  Server URI: urn:localhost:HumanOS:OpcUaServer
  Root browse name:  HumanOS

        │  OPC-UA subscription
        ▼
OPC-UA Client: connects and sees data from default
```

## Device: JSON Simulator

The `default` gateway uses the **FileReader** driver to read `C:\Temp\TestFile.json` as a JSON device.

### Data Model

The device exposes the following OPC-UA node hierarchy:

#### Top-level nodes

| Node         | JSON path      | Type  | Enabled outputs                   |
| :----------- | :------------- | :---- | :-------------------------------- |
| `Available`  | `$.Available`  | Int32 | AzureIoT, Influx, MQTT, REST, RMQ |
| `SignOfLife` | `$.SignOfLife` | Int32 | AzureIoT, Influx, MQTT, REST, RMQ |

#### Group: `Controller`

| Node               | JSON path            | Type            | Unit |
| :----------------- | :------------------- | :-------------- | :--- |
| `AlarmState`       | `$.AlarmState`       | Int32           |      |
| `OperationMode`    | `$.OperationMode`    | Int32           |      |
| `RunningState`     | `$.RunningState`     | Int32           |      |
| `PartCounter`      | `$.PartCounter`      | Int32           |      |
| `FeedrateOverride` | `$.FeedrateOverride` | Double          | %    |
| `SpindleOverride`  | `$.SpindleOverride`  | Double          | %    |
| `CuttingTime`      | `$.CuttingTime`      | Double (Stream) | min  |
| `OperationTime`    | `$.OperationTime`    | Double (Stream) | min  |
| `PowerOnTime`      | `$.PowerOnTime`      | Double (Stream) | min  |

#### Group: `Controller/NCPath1`

| Node                     | JSON path                  | Type   | Unit   |
| :----------------------- | :------------------------- | :----- | :----- |
| `CurrentFeed`            | `$.CurrentFeed`            | Double |        |
| `CurrentNcBlock`         | `$.CurrentNcBlock`         | String |        |
| `CurrentProgram`         | `$.CurrentProgram`         | String |        |
| `CurrentSequenceNr`      | `$.CurrentSequenceNr`      | String |        |
| `CurrentSpindleSpeed`    | `$.CurrentSpindleSpeed`    | Double | 1/min  |
| `CurrentToolId`          | `$.CurrentToolId`          | String |        |
| `MainProgram`            | `$.MainProgram`            | String |        |
| `MainProgramHeader`      | `$.MainProgramHeader`      | String |        |
| `ProgrammedFeed`         | `$.ProgrammedFeed`         | Double | mm/min |
| `ProgrammedSpindleSpeed` | `$.ProgrammedSpindleSpeed` | Double | 1/min  |

#### Alarm pool: `MachineAlarming`

| Task           | JSON path  | Retention | Sampling rate |
| :------------- | :--------- | :-------- | :------------ |
| `SystemAlarms` | `$.Alarms` | 720 h     | 2000 ms       |

### Sample `TestFile.json`

```json
{
  "Available": 1,
  "SignOfLife": 42,
  "AlarmState": 0,
  "OperationMode": 1,
  "RunningState": 1,
  "PartCounter": 17,
  "FeedrateOverride": 100.0,
  "SpindleOverride": 100.0,
  "CuttingTime": 12.5,
  "OperationTime": 30.0,
  "PowerOnTime": 480.0,
  "CurrentFeed": 250.0,
  "CurrentNcBlock": "N100 G01 X50",
  "CurrentProgram": "O0001",
  "CurrentSequenceNr": "N100",
  "CurrentSpindleSpeed": 3000.0,
  "CurrentToolId": "T01",
  "MainProgram": "O0001",
  "MainProgramHeader": "PART_A",
  "ProgrammedFeed": 300.0,
  "ProgrammedSpindleSpeed": 3500.0,
  "Alarms": []
}
```

## OPC-UA Server Configuration (`default` gateway)

| Setting          | Value                               |
| :--------------- | :---------------------------------- |
| Port             | `4840`                              |
| Server URI       | `urn:localhost:HumanOS:OpcUaServer` |
| Root browse name | `HumanOS`                           |
| Service rule     | Bind/unbind all nodes automatically |

## Prerequisites

- HumanOS IoT Designer ≥ 2.10
- A JSON file at `C:\Temp\TestFile.json` (see sample above)
- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html): recommended for browsing both OPC-UA servers

## See Also

- [OPC-UA Server Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [FileReader Driver Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FileReader/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
