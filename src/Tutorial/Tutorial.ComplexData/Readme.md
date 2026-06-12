---
id: tutorial-complex-data
title: "Tutorial: Complex Data"
subject: "Reading and exposing nested JSON data structures via OPC-UA in HumanOS"
keywords: [HumanOS, FileReader, JSON, OPC-UA, complex data, nested, data model, no-code]
---

# Tutorial: Complex Data

Shows how to read a **nested JSON file** and automatically expose every field as a typed OPC-UA node — without writing any C# script. The HumanOS FileReader driver traverses the JSON structure and maps each value to a corresponding node in the OPC-UA address space.

A **file-based simulator** is included (`C:\Temp\TestFile.json`), so no physical hardware is required.

## Architecture

```text
JSON File  (C:\Temp\TestFile.json)
        │  polled by FileReader driver
        ▼
Device: JSON Simulator  (type: JSON)
        │  driver parses nested structure
        │  maps each field to a typed data node
        ▼
OPC-UA Server (port 4840)
        │  exposes full node tree
        ▼
OPC-UA Client (e.g. UAExpert)
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.10
- A JSON file at `C:\Temp\TestFile.json` (create manually or copy the template from `DeviceTemplates/`)
- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) — recommended for browsing the OPC-UA output

## See Also

- [FileReader Driver Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FileReader/)
- [OPC-UA Server Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)