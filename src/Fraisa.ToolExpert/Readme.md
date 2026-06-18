---
id: usecase-fraisa-toolexpert
title: "Use Case: Fraisa ToolExpert"
subject: "Cloud REST API Integration via WebControl, Exposed as OPC-UA Service"
keywords: [HumanOS, Fraisa, ToolExpert, REST API, WebControl, OPC-UA, tool data, article number]
---

# Fraisa ToolExpert

Demonstrates how to expose a third-party cloud REST API as an OPC-UA service using HumanOS. The use case queries the [FRAISA ToolExpert](https://toolexpert.fraisa.com) product database by article number and makes the structured tool data available to OPC-UA clients.

## Architecture

```text
OPC-UA Client
      │  (calls OPC-UA command node)
      ▼
HumanOS OPC-UA Server
      │
      ▼
ToolExpert_ImportResourceData (WebControl script)
      │  HTTP GET → https://toolexpert.fraisa.com/...
      ▼
FRAISA ToolExpert REST API
      │  JSON response
      ▼
Parsed tool data returned to OPC-UA command output
```

## Prerequisites

- HumanOS IoT Runtime ≥ 2.11
- Outbound HTTPS access from the gateway to `toolexpert.fraisa.com`
- An OPC-UA client (e.g. [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html)) to invoke the command and inspect results

## Key Components

### Device Template: `ToolExpert_v1.json`

Defines one command node that accepts an article number as input and returns the parsed tool data as JSON output. Uses the **WebControl connector** for the HTTP transport.

### Script: `ToolExpert_ImportResourceData.cs`

A `TAbstractHttpScriptObject`-based script that:

1. Receives the article number (e.g. `P527930`) as input argument
2. Sends an HTTP GET request to the FRAISA ToolExpert API
3. Parses the JSON response and extracts product data fields
4. Returns the structured result to the command output port

## Processing Flow

1. An OPC-UA client calls the `ImportResourceData` command on the exposed service node, passing a FRAISA article number.
2. HumanOS routes the call through the WebControl connector to the FRAISA ToolExpert REST API.
3. `ToolExpert_ImportResourceData.cs` parses the API response and maps the relevant tool attributes to a JSON object.
4. The JSON result is returned to the OPC-UA client as the command output value.

## Usage Example

Article number input: `P527930`

The command returns a JSON object containing tool attributes (dimensions, material, geometry, etc.) as provided by the FRAISA ToolExpert API.

## See Also

- [WebControl Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.WebControl/)
- [OPC-UA Server Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
- [FRAISA ToolExpert](https://toolexpert.fraisa.com)
