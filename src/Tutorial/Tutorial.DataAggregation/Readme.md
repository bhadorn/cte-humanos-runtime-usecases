---
id: tutorial-data-aggregation
title: "Tutorial: Data Aggregation"
subject: "Aggregating multiple JSON data sources into a single OPC-UA node in HumanOS"
keywords: [HumanOS, FileReader, JSON, aggregation, OPC-UA, JSONAggregator, data model, no-code]
---

# Tutorial: Data Aggregation

Shows how to use the **JSONAggregator** device type to merge values from multiple JSON sources into a single, unified node in the OPC-UA address space — without writing any C# script.

A **file-based simulator** (`C:\Temp\TestFile.json`) is used as the data source.

## Step by Step Guide
In [Data Aggregation Step by Step Guide](https://doc.cybertech.swiss/runtime/Tutorials/Tutorial6/02_Example.md) you can find a step-by-step instruction of this tutorial.

## Architecture

```text
JSON File  (C:\Temp\TestFile.json)
        │  polled by FileReader driver
        ▼
Device: JSONSimulator  (type: JSONAggregator)
        │  driver aggregates all matching fields
        │  into a single merged node
        ▼
OPC-UA Server (port 4840)
        │  exposes the aggregated node tree
        ▼
OPC-UA Client (e.g. UAExpert)
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.10
- A JSON file at `C:\Temp\TestFile.json`
- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) — recommended for browsing the OPC-UA output

## See Also

- [FileReader Driver Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FileReader/)
- [OPC-UA Server Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)