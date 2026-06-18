---
id: humanos-runtime-usecases
title: "HumanOS Runtime UseCases"
subject: "HumanOS IoT Runtime — Integration Examples"
keywords: [HumanOS, IoT, Runtime, use cases, industrial automation, OPC-UA, MQTT, REST API, CNC]
---

# HumanOS Runtime UseCases

Collection of HumanOS Runtime examples demonstrating real-world integration patterns for industrial automation.

## Overview

Each use case is a self-contained [HumanOS IoT Designer](https://doc.cybertech.swiss/runtime/intro) project that can be deployed to an HumanOS IoT Runtime gateway. They cover a range of common integration scenarios: REST API automation, CNC machine connectivity, IoT messaging, and data-driven alerting.

## Required Tooling

**Essential**:

- [HumanOS IoT Designer](https://data.cybertech.swiss/public.php/dav/files/LgBzNjG2wtRPXFM/?accept=zip) — includes the trial runtime for local testing

**Depending on the use case**:

- **Ansible Agent**: Running [Semaphore](https://semaphoreui.com) instance with Ansible configured
- **FANUC Data Aggregation**: `FANUC NCGuide` simulator (pre-configured) or physical FANUC controller
- **FANUC Robot OPC-UA**: FANUC R-30iB Plus controller with OPC-UA option enabled
- **FLAMES Demo**: No additional hardware required (file-based simulator included)
- **Fraisa ToolExpert**: Network access to [toolexpert.fraisa.com](https://toolexpert.fraisa.com)
- **Heidenhain CSV Data Logger, Heidenhain OEE**: Heidenhain iTNC530 simulator with HeidenhainDNC configured (connection `ItncSim` → `localhost:19000`)
- **DMG MoriSeiki**: Reading data from older MoriSeiki machines
- **Siemens S7 Complex Data**: Siemens S7 PLC with the matching DataBlock configured
- **Sinumerik PowerLine**: Siemens Sinumerik 840D PL controller or simulator
- **SmartFactory Simulator**: [vHub](https://api.vhub.ch) API token for robot waypoint data (optional MQTT broker)
- **SparkPlug Alarming**: FANUC controller or simulator, MQTT broker (e.g. [Mosquitto](https://mosquitto.org)), [MQTT Explorer](https://mqtt-explorer.com)
- **Temperature Reporter**: No additional hardware required (file-based simulator included)
- **Timing Example**: No additional hardware required (file-based simulator included)

**Recommended general tooling**:

- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) — OPC-UA client for inspecting OPC-UA server output

## Use Cases

| Use Case                                                                           | Connector                         | Key Feature                                                                                                                 |
| :--------------------------------------------------------------------------------- | :-------------------------------- | :-------------------------------------------------------------------------------------------------------------------------- |
| [Ansible Agent](./src/AnsibleAgent/Readme.md)                                      | WebControl (REST)                 | IT automation via Semaphore API and HumanOS workflows                                                                       |
| [FANUC Data Aggregation](./src/FANUC.DataAggregation/Readme.md)                    | FanucControl                      | Aggregating CNC controller data into a unified OPC-UA entity                                                                |
| [FANUC Robot OPC-UA](./src/FanucRobotOpcUa/Readme.md)                              | OpcUaControl                      | OPC-UA bridge: FANUC R-30iB Plus robot telemetry re-published via HumanOS OPC-UA server                                     |
| [FLAMES Demo](./src/FLAMESDemo/Readme.md)                                          | FileReader + OPC-UA Server        | FLAMES OPC-UA companion spec: state machines, event handling, tool management, and dynamic FileSystem mapped via flames.xml |
| [Fraisa ToolExpert](./src/Fraisa.ToolExpert/Readme.md)                             | WebControl (REST)                 | Tool lookup from cloud API, exposed via OPC-UA                                                                              |
| [Heidenhain CSV data logger](./src/Heidenhain/Heidenhain_CSV_DataLogger/Readme.md) | HeidenhainControl + CSV Logger    | CSV data logging from Heidenhain iTNC530                                                                                    |
| [Heidenhain OEE](./src/Heidenhain/Heidenhain_OEE/Readme.md)                        | HeidenhainControl + OPC-UA Server | OEE data from Heidenhain iTNC530 via OPC-UA                                                                                 |
| [Mori Seiki](./src/MoriSeiki/Readme.md)                                            | TCP/IP Connector                  | Machine data acquisition (MDA) using native TCP/IP protocol                                                                 |
| [Siemens S7 Complex Data](./src/SiemensS7.ComplexData/Readme.md)                   | SiemensS7Control                  | Reading a typed entity struct from S7 DataBlock via OPC-UA                                                                  |
| [Sinumerik 840D PowerLine](./src/SinumerikPowerLine/Readme.md)                     | SinumerikControl                  | DNC file transfer and OEE/MDE data acquisition from CNC                                                                     |
| [SmartFactory Simulator](./src/SmartFactorySimulator/Readme.md)                    | FileReader + WebControl           | Full factory simulation: robots, test machines, storage, OEE                                                                |
| [SparkPlug Alarming](./src/SparkPlugAlarming/Readme.md)                            | FanucControl + SparkPlug          | Alarm forwarding from FANUC controller via MQTT SparkPlug                                                                   |
| [Temperature Reporter](./src/TemperatureReporter/Readme.md)                        | FileReader (simulator)            | Threshold-based alerting with multiple alarm levels                                                                         |
| [Timing Example](./src/TimingExample/Readme.md)                                    | FileReader (simulator)            | Example how to use timers to execute commands                                                                               |

## Tutorials

| Tutorial                                                                | Connector                     | Key Feature                                                             |
| :---------------------------------------------------------------------- | :---------------------------- | :---------------------------------------------------------------------- |
| [Coffee Report](./src/Tutorial/Tutorial.CoffeeReport)                   | FileReader + SQL Data Logger  | Custom C# script processing with dual-export to OPC-UA and MySQL        |
| [Command Workflow](./src/Tutorial/Tutorial.CommandWorkflow)             | HostControl                   | Commands, injection rules, and scripted multi-step workflows            |
| [Complex Data](./src/Tutorial/Tutorial.ComplexData)                     | FileReader (JSON)             | Reading nested JSON data structures and exposing them via OPC-UA        |
| [CSV Data Logger](./src/Tutorial/Tutorial.CSVDataLogger)                | FileReader + CSV Logger       | Logging device data to CSV files with a custom file-reader script       |
| [Data Aggregation](./src/Tutorial/Tutorial.DataAggregation)             | FileReader (JSONAggregator)   | Aggregating values from multiple JSON sources into a single OPC-UA node |
| [OPC-UA Bridge](./src/Tutorial/Tutorial.OPCUABridge)                    | FileReader + OPC-UA Server    | Bridging device data across two independent OPC-UA gateway instances    |
| [OPC-UA Bridge Extension](./src/Tutorial/Tutorial.OPCUABridgeExtension) | FileReader + OPC-UA Server    | Extending the OPC-UA bridge with a custom C# file-reader script         |
| [OPC-UA Bridge MDE](./src/Tutorial/Tutorial.MDE)                        | FileReader + OPC-UA Server    | Extending the OPC-UA bridge with a MDE structure                        |
| [Scripting](./src/Tutorial/Tutorial.Scripting)                          | WebControl (MTConnect) + MQTT | Custom payload processing for a Mazak CNC machine with MQTT forwarding  |
| [Web Service](./src/Tutorial/Tutorial.WebService)                       | WebService                    | Exposing gateway data via a REST endpoint with a custom payload script  |

## HumanOS Runtime

- [Reference Manual](https://doc.cybertech.swiss/runtime/intro)
- [Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
