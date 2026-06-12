---
id: fanuc-data-aggregation
title: "Use Case: FANUC — Data Aggregation"
subject: "Aggregating multiple FANUC CNC controller data nodes into a unified entity and exposing it via OPC-UA in HumanOS"
keywords: [HumanOS, FANUC, OPC-UA, data aggregation, CNC, controller, DataAggregator, no-code]
---

# Use Case: FANUC — Data Aggregation

Shows how to collect several individual **FANUC CNC controller signals** (availability, program name, operation mode, part counter) and combine them into a single **aggregated entity node** using the HumanOS DataAggregator processing network — without writing any C# script. The aggregated result is then exposed as a unified OPC-UA node.

A **FANUC NCGUIDE simulator** (`NCGUIDE!`) is pre-configured as the device, so no physical CNC hardware is required.

## Architecture

```text
FANUC CNC Controller (or NCGUIDE simulator)
        │  polled by FanucControl driver
        ▼
Device: FanucSimulator  (type: FANUC 0i-F, address: NCGUIDE!)
        │
        ├─ Controller group
        │    ├─ Available      (Int32)   — machine availability status
        │    ├─ MainProgram    (String)  — active NC program name
        │    ├─ OperationMode  (String)  — Manual / Automatic / MDI
        │    └─ PartCounter    (Int32)   — workpiece count
        │
        ▼
Processing Network: DataAggregator
        │  port matching rule combines individual nodes
        │  into one generic entity output
        ▼
DataNode: AggregatedData  (TGenericEntity)
        ▼
OPC-UA Server (port 4840)
        │  exposes unified aggregated node
        ▼
OPC-UA Client (e.g. UAExpert)
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.6
- `FANUC NCGuide`— included as the pre-configured simulator device
- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) — recommended for browsing the OPC-UA output

## See Also

- [FANUC Control Driver Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FanucControl/)
- [OPC-UA Server Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [Data Aggregation & Processing Networks](https://doc.cybertech.swiss/runtime/Development/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
