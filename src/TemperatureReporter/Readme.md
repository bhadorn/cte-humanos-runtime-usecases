---
id: usecase-temperature-reporter
title: "Use Case: Temperature Reporting using Threshold Triggers"
subject: "Threshold-Based Alerting with OnThreshold Triggers and TThresholdCalculator"
keywords: [HumanOS, threshold, OnThreshold, alerting, temperature, TThresholdCalculator, triggers, hysteresis, FileReader]
---

# Temperature Reporting using Threshold Triggers

Demonstrates the **OnThreshold trigger type** in HumanOS data nodes and processing ports. Instead of polling on a fixed interval, data is forwarded only when a value crosses a configured threshold — reducing unnecessary processing and network traffic. The use case also shows how to implement multiple alert levels using the `TThresholdCalculator` helper class.

A **file-based simulator** is included, so no physical hardware is required to run this example.

## Architecture

```text
Simulator (JSON file, FileReader plugin)
        │  reads temperature value
        ▼
Data Node  (TriggerType: OnThreshold)
        │  fires only when threshold is crossed
        ▼
AlertProcessingScript
   ├── TThresholdCalculator @ 50 °C  → Alert Level 1
   ├── TThresholdCalculator @ 60 °C  → Alert Level 2
   └── TThresholdCalculator @ 70 °C  → Alert Level 3
        │
        ▼
ReportProcessingScript  →  output / log
```

## Prerequisites

- HumanOS IoT Runtime ≥ 2.11
- No additional hardware required; the included simulator reads from a local JSON file

## Key Concepts

### OnThreshold Trigger Type

Data nodes and processing ports configured with `TriggerType: OnThreshold` only activate their downstream processing chain when the value exceeds (`Exceed`) or drops below (`Undercut`) the configured threshold. A `DebounceTime` prevents rapid re-triggering on noisy signals:

```json
"TriggerType": "OnThreshold",
"ThresholdSettings": {
  "DebounceTime": 5,
  "Exceed": 50.0,
  "Undercut": 48.0
}
```

In this example the data node fires at 50 °C (alarm on) and resets at 48 °C (hysteresis/alarm off).

### TThresholdCalculator

A built-in HumanOS helper class that tracks whether a value is currently above or below a threshold, including hysteresis. The `AlertProcessingScript` instantiates one calculator per alert level:

| Instance  | Threshold | Hysteresis        |
| :-------- | :-------: | :---------------- |
| `_alert1` |   50 °C   | threshold − 2 °C  |
| `_alert2` |   60 °C   | threshold − 2 °C  |
| `_alert3` |   70 °C   | threshold − 2 °C  |

Each calculator independently reports whether the temperature is currently in the alert zone, allowing fine-grained alert management without manual state tracking.

## Key Components

### Device Template — `Simulator_v1.json`

- Configures the **FileReader plugin** (`HumanOS.UHAL.FileReader`) as the data source
- Defines data nodes with `TriggerType: OnThreshold` and per-level threshold settings
- Wires the processing network: `AlertProcessingScript` feeds into `ReportProcessingScript`
- Groups alert levels (50 °C, 60 °C, 70 °C) into a nested node structure for clarity

### Script — `AlertProcessingScript.cs`

Receives the current temperature value, evaluates all three `TThresholdCalculator` instances, and outputs the active alert level (0 = none, 1/2/3 = corresponding level). Implements hysteresis to avoid alert flickering on noisy readings.

### Script — `ReportProcessingScript.cs`

Receives the alert level and the raw temperature value, logs the current state, and forwards the temperature value to the output port for downstream consumers (OPC-UA, historian, etc.).

## Processing Flow

1. The FileReader plugin reads the current temperature from the configured JSON file at the defined sampling rate.
2. The data node evaluates the `OnThreshold` condition. If the temperature has not crossed any threshold since the last check, processing stops here.
3. When a threshold crossing is detected, `AlertProcessingScript` determines the active alert level using the three `TThresholdCalculator` instances.
4. `ReportProcessingScript` logs the event and forwards the temperature value to configured outputs.

## Extending the Example

- **Add alert levels**: Instantiate additional `TThresholdCalculator` objects in `AlertProcessingScript` and add matching data nodes in the device template.
- **Connect to real hardware**: Replace the FileReader device with any HumanOS-supported connector (FANUC, Siemens, OPC-UA, etc.) and adjust the data node address.
- **Forward alerts**: Add an OPC-UA server, MQTT client, or historian plugin to the target configuration and connect the output port.

## See Also

- [Trigger Types Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.Runtime/GenericInformationModelNodes/DataNodes.md#trigger-types)
- [C# Scripting Guide](https://doc.cybertech.swiss/runtime/Development/)
- [Tutorial 4: C# Scripts](https://doc.cybertech.swiss/runtime/Tutorials/Tutorial4/)
