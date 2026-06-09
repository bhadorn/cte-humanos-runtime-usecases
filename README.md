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
- **Fraisa ToolExpert**: Network access to [toolexpert.fraisa.com](https://toolexpert.fraisa.com)
- **DMG MoriSeiki**: Reading data from older MoriSeiki machines
- **Sinumerik PowerLine**: Siemens Sinumerik 840D PL controller or simulator
- **SmartFactory Simulator**: [vHub](https://api.vhub.ch) API token for robot waypoint data (optional MQTT broker)
- **SparkPlug Alarming**: FANUC controller or simulator, MQTT broker (e.g. [Mosquitto](https://mosquitto.org)), [MQTT Explorer](https://mqtt-explorer.com)
- **Temperature Reporter**: No additional hardware required (file-based simulator included)
- **Timing Example**: No additional hardware required (file-based simulator included)

**Recommended general tooling**:

- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) — OPC-UA client for inspecting OPC-UA server output

## Use Cases

| Use Case                                                       | Connector                | Key Feature                                                 |
| :------------------------------------------------------------- | :----------------------- | :---------------------------------------------------------- |
| [Ansible Agent](./src/AnsibleAgent/Readme.md)                  | WebControl (REST)        | IT automation via Semaphore API and HumanOS workflows       |
| [Fraisa ToolExpert](./src/Fraisa.ToolExpert/Readme.md)         | WebControl (REST)        | Tool lookup from cloud API, exposed via OPC-UA              |
| [Mori Seiki](./src/MoriSeiki/Readme.md)                        | TCP/IP Connector         | Machine data acquisition (MDA) using native TCP/IP protocol |
| [Sinumerik 840D PowerLine](./src/SinumerikPowerLine/Readme.md) | SinumerikControl         | DNC file transfer and OEE/MDE data acquisition from CNC     |
| [SmartFactory Simulator](./src/SmartFactorySimulator/Readme.md) | FileReader + WebControl  | Full factory simulation: robots, test machines, storage, OEE |
| [SparkPlug Alarming](./src/SparkPlugAlarming/Readme.md)        | FanucControl + SparkPlug | Alarm forwarding from FANUC controller via MQTT SparkPlug   |
| [Temperature Reporter](./src/TemperatureReporter/Readme.md)    | FileReader (simulator)   | Threshold-based alerting with multiple alarm levels         |
| [Timing Example](./src/TimingExample/Readme.md)                | FileReader (simulator)   | Example how to use timers to execute commands               |

## HumanOS Runtime

- [Reference Manual](https://doc.cybertech.swiss/runtime/intro)
- [Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
