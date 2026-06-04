# Ansible Agent

Integrates [Ansible Semaphore](https://semaphoreui.com) — an open-source web UI and REST API wrapper for Ansible — into HumanOS Platform Pipelines. The agent provisions all required Semaphore resources (project, repository, environment, inventory, SSH key, playbook template) on the fly, starts a task, polls for completion, and returns structured JSON feedback to the HumanOS workflow engine.

## Architecture

```
HumanOS Workflow
      │
      ▼
UHAL_StartJob  ──► Login → GetOrCreate Project → GetOrCreate Repository
                        → GetOrCreate Environment → GetOrCreate Inventory
                        → GetOrCreate Key → GetOrCreate Template → StartTask
      │
      ▼
UHAL_GetJobStatus  ──► Poll Semaphore task status (running / success / error)
      │
      ▼
UHAL_GetJobOutput  ──► Extract JSON feedback from task output
```

The HumanOS **workflow engine** manages the full asynchronous lifecycle: start, wait for completion, fetch output.

## Prerequisites

- HumanOS IoT Runtime ≥ 2.11
- Running Semaphore instance accessible from the gateway (default: `http://192.168.0.55`)
- Semaphore credentials stored in the project's encrypted vault (`SemaphoreLogin`)
- Ansible playbooks already present in the configured Git repository

## Key Components

### Device Template — `SemaphoreAPI.json`

Defines the REST API interface using the **WebControl connector**. Each command maps to a Semaphore API endpoint. Data nodes expose job status and output to the HumanOS NodeSpace (and optionally to OPC-UA).

### Scripts (WebControl plugin)

| Script | Purpose |
|---|---|
| `Login.cs` | Authenticates with Semaphore and parses the session cookie |
| `Logout.cs` | Terminates the Semaphore session |
| `GetProjectByName.cs` | Looks up a project by name, returns its ID |
| `CreateProject.cs` | Creates a new Semaphore project |
| `GetRepositoryByName.cs` / `CreateOrUpdateRepository.cs` | Manages Git repository entries |
| `GetEnvironmentByName.cs` / `CreateOrUpdateEnvironment.cs` | Manages environment variable sets |
| `GetInventoryByName.cs` / `CreateOrUpdateInventory.cs` | Manages dynamic Ansible inventories |
| `GetKeyByName.cs` / `CreateOrUpdateKey.cs` | Manages SSH keys (stored Base64-encoded) |
| `GetTemplateByName.cs` / `CreateOrUpdateTemplate.cs` | Manages playbook template definitions |
| `StartTask.cs` | Launches a Semaphore task, returns the integer task ID |
| `GetTaskStatus.cs` | Retrieves current task status |
| `GetTaskOutput.cs` | Retrieves raw task log output |

### Orchestration Scripts

| Script | Purpose |
|---|---|
| `UHAL_StartJob.cs` | Full idempotent setup: logs in, ensures all Semaphore resources exist, starts the task |
| `UHAL_GetJobStatus.cs` | Polls task status with authenticated session; returns `running`, `success`, or `error` |
| `UHAL_GetJobOutput.cs` | Extracts JSON-structured feedback from task log output |

### Workflow — `JobOperation.cs`

An async HumanOS workflow that:
1. Calls `UHAL_StartJob` to provision resources and start the task
2. Polls `UHAL_GetJobStatus` until the task completes or fails
3. Calls `UHAL_GetJobOutput` and stores the result in the HumanOS data model

## Processing Flow

1. An external trigger (e.g. HumanOS Platform event or scheduled rule) starts the `JobOperation` workflow.
2. `UHAL_StartJob` logs into Semaphore, creates or resolves all required resources idempotently, and launches the playbook task. The returned task ID is passed to subsequent steps.
3. `UHAL_GetJobStatus` is called in a loop until the task reaches a terminal state.
4. `UHAL_GetJobOutput` parses the Ansible output for a JSON result block and writes it to the configured output data node.

## See Also

- [WebControl Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.WebControl/)
- [HumanOS Workflow Engine](https://doc.cybertech.swiss/platform/intro)
- [Ansible Documentation](https://docs.ansible.com/)
- [Semaphore UI](https://semaphoreui.com)
