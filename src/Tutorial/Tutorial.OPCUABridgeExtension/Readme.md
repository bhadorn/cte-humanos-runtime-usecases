---
id: tutorial-opcua-bridge-extension
title: "Tutorial: OPC-UA Bridge Extension"
subject: "Extending the OPC-UA bridge with a custom C# FileReader script in HumanOS"
keywords: [HumanOS, OPC-UA, bridge, FileReader, script, extension, UHAL, logic script]
---

# Tutorial: OPC-UA Bridge Extension

Extends the `Tutorial.OPCUABridge` pattern by adding a **custom C# logic script** to the FileReader plugin on the source gateway. The script intercepts the read cycle, loads the file content from a runtime-specified path, and returns it to the driver — demonstrating how to inject custom logic into the data-acquisition path before values are published via OPC-UA.

## Architecture

```text
JSON File  (path supplied at runtime)
        │
        ▼
Gateway: default  (port 4840)
  FileReader driver  →  calls ReadFile.cs (logic script)
        │  input:  "Name"    (file path)
        │  output: "Content" (raw file content)
        ▼
  Driver parses content  →  device data nodes
  OPC-UA Server  →  publishes nodes on opc.tcp://localhost:4840
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.10
- A JSON file accessible at the path configured in the device properties
- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) — recommended for browsing the OPC-UA output

## See Also

- [C# Scripting Guide](https://doc.cybertech.swiss/runtime/Development/)
- [FileReader Driver Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FileReader/)
- [OPC-UA Server Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
