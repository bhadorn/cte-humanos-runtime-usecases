---
id: usecase-sparkplug-alarming
title: "Use Case: SparkPlug Configuration for Alarming"
subject: "FANUC Alarm Forwarding via MQTT SparkPlug B"
keywords: [HumanOS, FANUC, SparkPlug, SparkPlug B, MQTT, alarming, alarm forwarding, FanucControl, IoT messaging]
---

# SparkPlug Configuration for Alarming

Demonstrates how to forward machine alarms from a **FANUC** controller to MQTT clients using the **SparkPlug B** protocol. The SparkPlug standard does not define a dedicated alarm message type, so this use case encodes active alarms as a JSON array and transmits it as a regular SparkPlug metric that updates whenever the alarm state changes.

## Architecture

```text
FANUC Controller (192.168.0.1:8193)
        │  FanucControl connector
        ▼
AlarmEvent Pool  ←  System Alarm Task
        │  (collects active alarms)
        ▼
AlarmProcessorScript  (runs every 30 s)
        │  serializes alarms → JSON array
        ▼
Data Node  (alarm stream value)
        │  OnChange trigger
        ▼
SparkPlug Client  →  MQTT Broker  →  SparkPlug subscribers
```

## Prerequisites

- HumanOS IoT Runtime ≥ 2.10
- FANUC controller accessible from the gateway (default: `192.168.0.1`, port `8193`)
- MQTT broker reachable from the gateway (e.g. [Mosquitto](https://mosquitto.org))
- Optional: [MQTT Explorer](https://mqtt-explorer.com) for inspecting published SparkPlug metrics

## Key Components

### Device Template — `FanucAlarms_v1.json`

Configures:

- A **FANUC Rack** device connecting to the controller via the FanucControl connector
- An **AlarmEvent Pool Node** that subscribes to machine alarms from the controller
- A **System Alarm Task** that activates the alarm collection pipeline
- A **data node** that holds the current alarm stream as a JSON string
- A **SparkPlug Client** plugin that publishes the data node value as a SparkPlug metric on change

### Script — `AlarmProcessorScript.cs`

Runs on a 30-second cycle. Reads all active alarms from the AlarmEvent Pool, serializes them into a JSON array (preserving alarm code, message, and severity), and writes the result to the output port connected to the alarm stream data node.

Example output:

```json
[
  { "Code": 1001, "Message": "Servo alarm: axis 1",         "Severity": "High"   },
  { "Code": 2005, "Message": "Overtravel: axis 2 positive", "Severity": "Medium" }
]
```

If no alarms are active, the script outputs an empty array `[]`.

## Processing Flow

1. The FANUC connector continuously monitors the controller at `192.168.0.1:8193` and populates the AlarmEvent Pool with any active alarms.
2. Every 30 seconds, the `AlarmProcessorScript` reads the current alarm pool contents.
3. The script serializes the alarms to a JSON array string and writes it to the output port.
4. The output port is connected to a data node configured with an **OnChange** trigger.
5. When the alarm state changes (new alarm or alarm cleared), the SparkPlug Client detects the data node change and publishes an updated SparkPlug DDATA message to the MQTT broker.
6. Any SparkPlug-compliant subscriber receives the updated alarm list in real time.

## Configuration

The MQTT broker address and SparkPlug group/node IDs are configured in the SparkPlug Client plugin settings within the target configuration. Adjust the FANUC device address in the `Devices/` folder to match your controller's IP.

## See Also

- [SparkPlug Client Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.SparkPlugClient/)
- [FANUC Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FanucControl/)
- [SparkPlug B Specification](https://sparkplug.eclipse.org/specification/version/3.0/)
