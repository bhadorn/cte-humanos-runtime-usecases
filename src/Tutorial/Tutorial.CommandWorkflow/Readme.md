---
id: tutorial-command-workflow
title: "Tutorial: Command Workflow"
subject: "Triggering multi-device workflows from a command with injection rules in HumanOS"
keywords: [HumanOS, workflow, command, injection, rule, schema, HostControl, scripting]
---

# Tutorial: Command Workflow

Demonstrates how to trigger a **named workflow** on every connected device from a single command, and how to dynamically inject data-model nodes into a device using an **injection rule** and a **schema**.

No physical hardware is required: two `HostControl` devices simulate the connected targets.

## Step by Step Guide

In [Command Workflow Step by Step Guide](https://doc.cybertech.swiss/runtime/Tutorials/Tutorial5/Example01.md) you can find a step-by-step instruction of this tutorial.

## Architecture

```text
Command: ExecuteWorkflow (ExecuteWorkflow.cs)
        │  input: Arg1 (string)
        │  iterates all devices with a DriverId property
        ▼
Workflow: Operation_One (Operation_One.cs)
        │  reads Arg1 from context
        │  logs greeting message
        ▼
OPC-UA Server (port 4840): exposes all device nodes

InjectionRule (DataModel/Rules/InjectionRule.json)
        │  triggered on device detection
        ▼
Rule Script: InjectToDevices.cs
        │  creates nodes from InjectionSchema
        ▼
Device node: schema nodes appended at runtime
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.10
- No additional hardware required

## See Also

- [Workflows Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.Runtime/GenericInformationModelNodes/Workflows.md)
- [Rules Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.Runtime/GenericInformationModelNodes/Rules.md)
- [C# Scripting Guide](https://doc.cybertech.swiss/runtime/Development/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
