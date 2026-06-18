---
id: usecase-timing-example
title: "Use Case: Timer-Driven Command Execution"
subject: "Periodic Command Execution using HumanOS Timers, GenericRules, and TCommandHelper"
keywords: [HumanOS, timer, OnTimer, GenericRule, TCommandHelper, scheduling, FileReader, rule script]
---

# Timer-Driven Command Execution

Demonstrates how to use **HumanOS Timers** to execute commands on a configurable periodic schedule. A timer fires at a fixed interval, triggering a rule whose C# script iterates over all connected devices and issues a `ReadFile` command to each: showing the general pattern for any repeating, device-spanning operation.

A **file-based simulator** is included, so no physical hardware is required to run this example.

## Architecture

```text
Timer (2 s interval, infinite repeat)    [MyTimer.json]
        │  fires OnTimer event
        ▼
GenericRule  →  Timer_Script.cs
        │  Kernel.NodeSpace.queryNodes(hasProperty("DriverId"))
        │  TCommandHelper.call(Device, "ReadFile", Args)
        ▼
FileReader Device (Simulator)  →  reads File01.txt  →  logs result
```

## Prerequisites

- HumanOS IoT Runtime ≥ 2.11
- No additional hardware required; the included simulator reads from a local text file

## Key Concepts

### Timer

A `Timer` is defined in the data model with a millisecond `Interval` and a `RepeatCount`. Setting `RepeatCount` to `2147483647` (Int32.MaxValue) makes the timer run indefinitely:

```json
{
  "Name": "Timer",
  "TriggerConfig": {
    "Interval": 2000,
    "RepeatCount": 2147483647
  }
}
```

### OnTimer Trigger and GenericRule

A `GenericRule` with `TriggerEvent: OnTimer` executes its action every time the timer fires. The action points to a C# script file:

```json
{
  "Name": "Test.ScriptedRule1",
  "Action": {
    "ScriptFile": "Timer_Script.cs",
    "Type": "ScriptFile"
  },
  "TriggerEvent": "OnTimer",
  "Type": "GenericRule"
}
```

An optional `TriggerCondition` expression can be set to restrict execution to specific runtime states.

### Querying Devices at Runtime

Inside the rule script, `Kernel.NodeSpace.queryNodes` searches the live device graph. Filtering by `DriverId` selects all actively connected devices regardless of their type:

```csharp
foreach (IGroupRelation Device in Kernel.NodeSpace.queryNodes(n => n.hasProperty("DriverId")))
{
    TCommandArgs Args = new TCommandArgs();
    Args.Input["Name"] = "path/to/File01.txt";
    TCommandResult Result = TCommandHelper.call(Device, "ReadFile", Args);
    if (Result.State == EProcessingState.Good)
        Logger.writeInfo($"File of '{Device.Name}' read.");
    else
        Logger.writeError($"Failed to read file. {Result.ErrorMessage}");
}
```

`TCommandHelper.call` dispatches the command synchronously and returns an `EProcessingState.Good` result on success.

## Key Components

### Data Model: `MyTimer.json`

Defines the `Timer` object (2 s interval, infinite repeat) and the associated `GenericRule` that binds the `OnTimer` event to `Timer_Script.cs`. Both are placed in the `default` environment's `DataModel/Objects` folder.

### Device Template: `Simulator_v1.json`

Configures the **FileReader plugin** (`HumanOS.UHAL.FileReader`) as the data source and exposes a single `ReadFile` command. The command accepts a `Name` input (file path) and returns a `Content` output (file content as string).

### Script: `Timer_Script.cs`

Queries all devices with a `DriverId` property and calls `ReadFile` on each. Logs success or failure for every device. This pattern scales to any command available on the matched devices.

## Processing Flow

1. The runtime starts the timer as defined in `MyTimer.json`.
2. Every 2 seconds the timer fires an `OnTimer` event.
3. The `GenericRule` triggers `Timer_Script.cs`.
4. The script queries the node space for all devices that expose a `DriverId` property.
5. For each device, it calls the `ReadFile` command with the path to `File01.txt`.
6. The file content is returned; success or failure is logged.

## Extending the Example

- **Change the interval**: Adjust `Interval` (ms) in `MyTimer.json` to suit the required polling frequency.
- **Replace the command**: Substitute `ReadFile` with any other device command: read sensor values, trigger a write, or call an HTTP endpoint.
- **Add a condition**: Set `TriggerCondition` on the rule to a logical expression so the script only runs when a runtime condition is met.
- **Process results**: Forward command outputs to a data node, OPC-UA server, or historian by wiring output ports in the processing network.
- **Connect real hardware**: Replace the FileReader device with any HumanOS-supported connector (FANUC, Sinumerik, OPC-UA, etc.) and adjust the command name and arguments accordingly.

## See Also

- [Timers Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.Runtime/Timers/)
- [Rules Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.Runtime/Rules/)
- [C# Scripting Guide](https://doc.cybertech.swiss/runtime/Development/)
- [Tutorial 4: C# Scripts](https://doc.cybertech.swiss/runtime/Tutorials/Tutorial4/)
