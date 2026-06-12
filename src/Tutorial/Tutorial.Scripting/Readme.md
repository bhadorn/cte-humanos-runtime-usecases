---
id: tutorial-scripting
title: "Tutorial: Scripting"
subject: "Custom payload processing for MTConnect and MQTT using C# scripts in HumanOS"
keywords: [HumanOS, MTConnect, MQTT, scripting, WebControl, payload, Mazak, CNC, data logger, command]
---

# Tutorial: Scripting

Demonstrates three scripting patterns in a single project: a **WebControl HTTP stream script** that extracts fields from an MTConnect XML response, an **MQTT publisher script** that formats device data as JSON for a specific topic, and a **global command script** as a minimal callable entry point.

The data source is the public Mazak MTConnect demo server at `http://mtconnect.mazakcorp.com:5719`.

## Architecture

```text
Mazak MTConnect Server  (http://mtconnect.mazakcorp.com:5719)
        │  HTTP polling via WebControl driver
        ▼
MTConnectPayloadProcessor.cs
        │  parses XML response (/current endpoint)
        │  extracts OperationMode (string) → Node "OperationMode"
        │  extracts PartCountAct (int)    → Node "PartCounter"
        ▼
Device: Mazak  — live data nodes in node space

        │  MQTT publisher subscribes to nodes with EnableMqtt=true
        ▼
MqttPayloadScript.cs
        │  serialises each data set as JSON
        │  sets topic to "mazak/data/{Name}"
        ▼
MQTT Broker (localhost)  →  topic: mazak/data/<node-name>

OPC-UA Server (port 4840)   — exposes all nodes in parallel
WebService    (REST)        — exposes gateway via HTTP
Global Command: Command1    — example callable command
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.10
- Internet access to `http://mtconnect.mazakcorp.com:5719` (public Mazak demo)
- A running MQTT broker on `localhost` (e.g. [Mosquitto](https://mosquitto.org)) for the MQTT output
- [MQTT Explorer](https://mqtt-explorer.com) — recommended to inspect published messages

## See Also

- [WebControl Driver Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.WebControl/)
- [MQTT Client Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.MQTTClient/)
- [C# Scripting Guide](https://doc.cybertech.swiss/runtime/Development/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)