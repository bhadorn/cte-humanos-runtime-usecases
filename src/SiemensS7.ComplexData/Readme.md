---
id: siemenss7-complex-data
title: "Use Case: Siemens S7 — Complex Data"
subject: "Reading structured entity data from a Siemens S7 PLC and exposing it via OPC-UA in HumanOS"
keywords: [HumanOS, Siemens S7, OPC-UA, complex data, entity type, DataBlock, struct, no-code]
---

# Use Case: Siemens S7 — Complex Data

Shows how to read a **structured data block from a Siemens S7 PLC** and automatically map it to a typed OPC-UA node using a custom **EntityType** — without writing any C# script. The HumanOS SiemensS7Control driver reads a raw byte array from the PLC DataBlock, deserializes it into a strongly-typed entity (`ComplexTypeA`), and exposes every field as a named OPC-UA node.

## Architecture

```text
Siemens S7 PLC
        │  DataBlock memory: Plc1.DataBlock.ByteArray:2.20[100]
        │  byte order inverted (big-endian → little-endian)
        ▼
Device Template: SiemensS7_v1
        │  DataNode: ReadComplexData
        │  EntityType: ComplexTypeA
        │    ├─ Id           (Guid)
        │    ├─ OrderNumber  (String, 32 chars)
        │    ├─ PartCount    (Int32)
        │    └─ CycleTime    (Single)
        ▼
OPC-UA Server (port 4840)
        │  exposes full typed node tree
        ▼
OPC-UA Client (e.g. UAExpert)
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.6
- A reachable Siemens S7 PLC with the matching DataBlock configured
- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) — recommended for browsing the OPC-UA output

## See Also

- [Siemens S7 Control Driver Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.SiemensS7Control/)
- [OPC-UA Server Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [Entity Types & Complex Data](https://doc.cybertech.swiss/runtime/Development/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
