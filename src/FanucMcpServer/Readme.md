---
id: usecase-fanuc-mcp-server
title: "Use Case: FANUC MCP Server"
subject: "Exposing a FANUC CNC controller as a Model Context Protocol (MCP) server so AI agents can read live machine data and invoke gateway tools"
keywords: [HumanOS, MCP, Model Context Protocol, AI agent, LLM, FANUC, CNC, SSE, tools, skills, OEE, McpServer, PeSeL]
---

# FANUC MCP Server

Shows how to turn a HumanOS IoT gateway into a **Model Context Protocol (MCP) server**, so that an
AI agent or LLM (Claude, ChatGPT, a custom agent, …) can connect over HTTP/SSE, **read live FANUC CNC
data** (machine state, program, part count, alarms, …) and **invoke gateway tools** such as reading an
NC file from the controller.

The MCP endpoint is provided by the **HumanOS MCP Server plugin** (`HumanOS.PeSeL.McpServer`). The
machine data and commands come from a single FANUC device (`FanucOEE` template) connected to the
**FANUC NCGUIDE simulator** (`NCGUIDE!`), so no physical CNC hardware is required. With
`ExpandToolsToNodeSpace` enabled, the device's entire node space — every data node, command and skill —
is automatically published as MCP tools the agent can discover and call.

## Architecture

```text
   AI Agent / LLM                       HumanOS IoT Runtime (Gateway)                    FANUC Controller
┌──────────────────┐                 ┌──────────────────────────────────────────┐    ┌──────────────────┐
│  MCP client       │  HTTP / SSE     │  HumanOS.PeSeL.McpServer  "FactoryFloor"   │    │                  │
│  (Claude, custom  │◀──────────────▶│    GET  /sse      (event stream)            │    │  FANUC 0i-TF      │
│   agent, …)       │  :8080          │    POST /message  (tool calls)             │    │  (NCGUIDE         │
│                   │                 │           │  ExpandToolsToNodeSpace         │    │   simulator)      │
│  • list tools     │                 │           ▼                                 │    │                  │
│  • read nodes     │                 │  Device: FANUC TestRack (FanucOEE)          │◀──▶│  polled by        │
│  • call ReadFile  │                 │    ├─ Controller (state, program, counters) │    │  FanucControl     │
└──────────────────┘                 │    ├─ DataProcessors (OEE machine state)    │    └──────────────────┘
                                      │    └─ Skill: ProgramManagement → ReadFile   │
                                      └──────────────────────────────────────────┘
```

The agent connects to the SSE endpoint, receives the list of available tools (derived from the FANUC
device node space), and can then read values or invoke commands. Tool calls are translated by the MCP
server plugin into reads/writes/command-invocations against the gateway's data model.

## Project Layout

| Path                                                  | Purpose                                                                                  |
| :---------------------------------------------------- | :--------------------------------------------------------------------------------------- |
| `FanucMcpServer.h2proj`                               | HumanOS IoT Designer project file (target, plugins, device instance)                     |
| `DeviceTemplates/FanucOEE.json`                       | Device template: FANUC data nodes, `ReadFile` command, `ProgramManagement` skill and the OEE processing network |
| `default/`                                            | Default IoT Gateway target                                                               |
| `default/Devices/FANUC TestRack.json`                 | Concrete FANUC device instance (address `NCGUIDE!`, type `0i-TF`)                        |
| `default/HumanOS.PeSeL.McpServer/settings.json`       | MCP server service (`FactoryFloor`) — endpoint, transport paths, CORS and tool exposure  |
| `default/HumanOS.UHAL.FanucControl/`                  | FANUC control driver config and the `Fanuc_ProductProcessor.cs` script                   |
| `default/HumanOS.UHAL.DeviceDetectors/`               | Device-detector plugin (enabled)                                                         |
| `Build/`                                              | Deployment-ready, published artifact for the `default` target (incl. service scripts)    |
| `globalids.json`                                      | Stable global node ids for the published data model                                      |

## The MCP Server (`HumanOS.PeSeL.McpServer`)

The plugin hosts one MCP service, configured in `default/HumanOS.PeSeL.McpServer/settings.json`:

| Setting                     | Value         | Meaning                                                                          |
| :-------------------------- | :------------ | :------------------------------------------------------------------------------- |
| `Name`                      | `FactoryFloor`| Logical name of the MCP service                                                  |
| `HostName` / `Port`         | `localhost` / `8080` | Address the MCP server listens on                                          |
| `SsePath`                   | `/sse`        | Server-Sent-Events stream the MCP client subscribes to (`GET`)                   |
| `MessagePath`               | `/message`    | Endpoint the client posts tool/JSON-RPC messages to (`POST`)                     |
| `ExpandToolsToNodeSpace`    | `true`        | Auto-publishes the device node space (data nodes, commands, skills) as MCP tools |
| `AllowedOrigins`            | `["*"]`       | CORS — open to any origin (tighten for production)                               |
| `AuthenticationSecretName`  | `""`          | No authentication (set a secret name to require a bearer token)                  |
| `DataAccessTimeoutSeconds`  | `5.0`         | Max time to wait when reading/writing a node for a tool call                     |
| `X509CertificateFile` / `…Password` | `""`  | Optional TLS certificate to serve the endpoint over HTTPS                         |
| `Tools`                     | `[]`          | Explicit tool list — empty because tools are expanded from the node space        |

The MCP endpoint for an agent is therefore:

```
http://localhost:8080/sse        (SSE event stream)
http://localhost:8080/message    (JSON-RPC message POST)
```

## The FANUC Device (`FanucOEE`)

The `FANUC TestRack` device (template `FanucOEE`, FANUC type `0i-TF`, address `NCGUIDE!`) supplies the
data and tools the agent sees. The most relevant items:

| Node / Command / Skill          | Type      | Purpose                                                                  |
| :------------------------------ | :-------- | :----------------------------------------------------------------------- |
| `OEEMachineState` / `…Name`     | Int / Str | Derived OEE machine state (e.g. 200 = *Production*, 900 = *Power Off*)    |
| `Available`                     | Int       | Machine availability                                                     |
| `Controller/OperationMode`      | Int       | Manual / Automatic / MDI                                                 |
| `Controller/RunningState`       | Int       | Program execution state (active / feed-hold / stopped)                   |
| `Controller/PartCounter`        | Int       | Workpiece count                                                          |
| `Controller/NCPath1/MainProgram`| Str       | Active NC program                                                        |
| `OEEProductName` / `…Step`      | Str       | Product / production step parsed from the program header                 |
| `MachineAlarming`               | Events    | NC system alarm/event pool                                               |
| `ReadFile` (command)            | command   | Reads an NC file from the controller (args: `Name`, `Type`; out `Content`) |
| `ProgramManagement` (skill)     | skill     | Groups program-related tools (currently `ReadFile`) for the agent        |

A small **DataProcessors** processing network turns raw controller signals into the OEE machine state
and product/step values:

| Processor                  | Type                         | Role                                                                          |
| :------------------------- | :--------------------------- | :---------------------------------------------------------------------------- |
| `MachineStateProcessor`    | `SituationProcessingNode`    | Maps availability / alarm / mode / running-state into an OEE state + name     |
| `ProductProcessor`         | `CSharpScriptProcessingNode` | `Fanuc_ProductProcessor.cs` parses product name + step from the program header |
| `FeedrateOverrideProcessor`| `SituationProcessingNode`    | Converts the raw PMC feed-rate-override byte into a percentage                 |
| `SpindleOverrideProcessor` | `SituationProcessingNode`    | Converts the raw PMC spindle-override byte into a percentage                   |

Processors and data nodes are wired by **port matching** (`PortMatchId`) rather than explicit links.

## How an Agent Uses It

1. Start the gateway (NCGUIDE simulator running). The MCP server begins listening on `:8080`.
2. The agent connects to `http://localhost:8080/sse` and performs the MCP handshake.
3. The agent calls `tools/list` and discovers the FANUC nodes, the `ReadFile` command and the
   `ProgramManagement` skill (expanded from the node space).
4. The agent reads values (e.g. *"is the machine in production?"* → `OEEMachineStateName`) or invokes
   `ReadFile` to fetch an NC program from the controller.
5. Each tool call is resolved by the MCP server against the live FANUC data model within
   `DataAccessTimeoutSeconds`.

## Prerequisites

- HumanOS IoT Runtime ≥ 2.11 with the **MCP Server plugin** (`HumanOS.PeSeL.McpServer`) and the
  **FANUC Control plugin** (`HumanOS.UHAL.FanucControl`) licensed and installed
- `FANUC NCGuide` simulator (the device is pre-configured for address `NCGUIDE!`) — or a physical FANUC
  controller reachable at the configured address
- An **MCP client**: an MCP-capable AI agent/LLM, or a generic MCP inspector/test client to browse the
  tools and read nodes

## Configuration

- **Endpoint / transport**: edit `default/HumanOS.PeSeL.McpServer/settings.json` to change `HostName`,
  `Port`, `SsePath` or `MessagePath`.
- **Security**: for anything beyond local testing, restrict `AllowedOrigins`, set
  `AuthenticationSecretName` (with a matching secret in the project's secret store) to require a bearer
  token, and configure `X509CertificateFile` / `X509CertificatePassword` to serve over HTTPS.
- **Exposed tools**: keep `ExpandToolsToNodeSpace = true` to publish the whole node space, or set it to
  `false` and list specific tools explicitly under `Tools` to limit what the agent can see.
- **Target controller**: in the `default` target, edit `Devices/FANUC TestRack.json` (and the device
  entry in `FanucMcpServer.h2proj`) and set `Address` to your controller (default `NCGUIDE!` for the
  simulator) and the FANUC `Type`.
- **Deployment**: the `Build/` folder contains the published, deployment-ready target, including
  `RegisterService.ps1` / `UnRegisterService.ps1` to install the gateway as a Windows service.

## See Also

- [HumanOS Runtime Reference Manual](https://doc.cybertech.swiss/runtime/intro)
- [MCP Server Plugin Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.McpServer/)
- [FANUC Control Driver Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.FanucControl/)
- [Model Context Protocol](https://modelcontextprotocol.io)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
