---
id: tutorial-csv-data-logger
title: "Tutorial: CSV Data Logger"
subject: "Logging device data to CSV files using a custom FileReader script in HumanOS"
keywords: [HumanOS, CSV, data logger, FileReader, script, CsvDataLogger, logging, persistence]
---

# Tutorial: CSV Data Logger

Shows how to combine a **custom FileReader script** with the **CSV Data Logger** plugin to read data from a device and persist it continuously to a CSV file. The `ReadFile.cs` script demonstrates the UHAL logic script pattern — receiving a file path as an input argument and returning the file content as an output argument.

A **file-based simulator** (`C:\Temp\TestFile.json`) replaces physical hardware.

## Step by Step Guide
In [CSV Data Logger Step by Step Guide](https://doc.cybertech.swiss/runtime/Tutorials/Tutorial3/02_Example.md) you can find a step-by-step instruction of this tutorial.

## Architecture

```text
JSON File  (C:\Temp\TestFile.json)
        │  polled by FileReader driver
        ▼
Device: Simulator  (type: JSONFile)
        │  custom logic script: ReadFile.cs
        │  reads file path from input argument
        │  returns content as output argument
        ▼
CsvDataLogger plugin
        │  subscribes to device data nodes
        │  appends rows on every value change
        ▼
CSV File  (configured output path)

OPC-UA Server (port 4840)  —  exposes device nodes in parallel
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.10
- A JSON file at `C:\Temp\TestFile.json`
- No additional hardware required

## See Also

- [CSV Data Logger Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.CsvDataLogger/)
- [FileReader Driver Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FileReader/)
- [C# Scripting Guide](https://doc.cybertech.swiss/runtime/Development/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)