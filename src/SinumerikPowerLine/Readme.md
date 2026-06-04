---
id: usecase-sinumerik-powerline
title: "Use Case: Sinumerik 840D PowerLine"
subject: "CNC DNC Communication and OEE/MDE Data Acquisition"
keywords: [HumanOS, Siemens, Sinumerik, 840D, DNC, OEE, MDE, CNC, SinumerikControl, NC programs, tool data]
---

# Sinumerik 840D PowerLine

Shows how to connect a **Siemens Sinumerik 840D PL** CNC controller to HumanOS using the SinumerikControl connector. The project provides two independent device templates covering the two most common integration scenarios: DNC file exchange and production data acquisition (OEE/MDE).

## Architecture

```text
Sinumerik 840D PL (s840d.pl://<ip>)
        │
        │  SinumerikControl connector
        ▼
HumanOS IoT Runtime
   ├── Sinumerik_DNC  → DNC Client → NC program / tool data exchange
   └── Sinumerik_OEE  → NodeSpace  → OPC-UA / MQTT / Database
```

## Prerequisites

- HumanOS IoT Runtime ≥ 2.9
- SinumerikControl plugin licensed and installed on the gateway
- Network access from the gateway to the Sinumerik controller (default: `10.1.6.2`, protocol `s840d.pl://`)
- For DNC: DNC Client plugin configured on the gateway
- OPC-UA client (e.g. [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html)) for inspecting acquired data

## Device Templates

### `Sinumerik_DNC.json` — DNC Communication

Handles bi-directional exchange of NC programs and tool data between the controller and a DNC system. Key data nodes cover:

- NC program upload and download
- Tool data read and write
- Program directory listing

This template uses the **HumanOS DNC information model** (`HumanOS.DNC`) and integrates with the DNC Client plugin for file management workflows.

### `Sinumerik_OEE.json` — OEE and MDE Data Acquisition

Acquires real-time production data from the controller and maps it to the **HumanOS OEE information model** (`HumanOS.OEE`). Typical data points include:

- Machine operating state (running, idle, alarm, setup)
- Active NC program name
- Alarm/fault events
- Axis positions and feed rate
- Spindle speed and load

The acquired data is exposed via OPC-UA and can be forwarded to databases or dashboards for OEE calculation.

## Processing Flow

### DNC Path

1. An external DNC client (or HumanOS workflow) requests a file operation (upload/download).
2. HumanOS routes the request through the Sinumerik connector to the controller.
3. The result is returned to the requesting client.

### OEE/MDE Path

1. HumanOS polls the configured data nodes on the controller at the defined sampling rate.
2. Acquired values are written to the HumanOS NodeSpace.
3. Rules and processing scripts map raw controller values to standardized OEE states.
4. The mapped data is forwarded to OPC-UA, MQTT, or a historian.

## Configuration

The `default` target configuration points to `10.1.6.2` via the `s840d.pl://` protocol. Adjust the device address in the `Devices/` folder to match your controller's IP address. The `Build/` folder contains deployment-ready artifacts for both default and secret variants.

## See Also

- [Sinumerik Connector Manual](https://doc.cybertech.swiss/runtime/2.11/Manuals/HumanOS.UHAL.SinumerikControl/)
- [HumanOS OEE Model](https://doc.cybertech.swiss/runtime/Models/HumanOS.OEE/)
- [HumanOS DNC Model](https://doc.cybertech.swiss/runtime/Models/HumanOS.DNC/)
- [DNC Client Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.DncClient/)
