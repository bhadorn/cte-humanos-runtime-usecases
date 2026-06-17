---
id: tutorial-opcua-bridge
title: "Tutorial: OPC-UA Bridge"
subject: "Bridging device data across two independent OPC-UA gateway instances in HumanOS"
keywords: [HumanOS, OPC-UA, bridge, gateway, FileReader, multi-gateway, port, no-code]
---

# Tutorial: OPC-UA Bridge

Shows how to forward data from one HumanOS gateway to a second gateway purely over OPC-UA — without any custom scripting. The `default` gateway reads a JSON file and publishes the data on an OPC-UA server (port 4840). The `Gateway1` gateway subscribes to that server and re-publishes the data on its own OPC-UA server (port 4841).

This is the foundational pattern for multi-hop data distribution, DMZ bridging, or aggregating data from edge gateways into a central gateway.

## Step by Step Guide
In [OPC-UA Bridge Step by Step Guide](https://doc.cybertech.swiss/runtime/Tutorials/Tutorial1/02_Example.md) you can find a step-by-step instruction of this tutorial.

## Architecture

```text
JSON File  (C:\Temp\TestFile.json)
        │
        ▼
Gateway: default  (port 4840)
  FileReader driver  →  JSON device nodes
  OPC-UA Server  →  publishes nodes on opc.tcp://localhost:4840

        │  OPC-UA subscription
        ▼

Gateway: Gateway1  (port 4841)
  OPC-UA Server  →  re-publishes received nodes on opc.tcp://localhost:4841
        │
        ▼
OPC-UA Client — connects to port 4841, sees data from default
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.10
- A JSON file at `C:\Temp\TestFile.json`
- [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html) — recommended for browsing both OPC-UA servers

## See Also

- [OPC-UA Server Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [FileReader Driver Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FileReader/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)