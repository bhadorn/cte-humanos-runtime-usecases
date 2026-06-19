# HumanOS.Dnc.Client

OPC-UA client application that drives the DNC commands published by the
[DNC gateway](../Readme.md). It connects to the gateway's OPC-UA server and performs file, tool
and directory operations against each configured machine through the `HumanOS.DNC` command nodes.

## Settings

The `appsettings.json` must be placed in the following folder:

```
C:\ProgramData\CyberTech\HumanOS.Dnc.Client
```

It contains the OPC-UA client defaults and the list of machine configurations.

### `OpcUaClient.Common`

Shared OPC-UA client options applied to every machine:

| Key                              | Example       | Description                                            |
| :------------------------------- | :------------ | :----------------------------------------------------- |
| `opc:StoreType`                  | `Directory`   | Certificate store type                                 |
| `opc:SecuritySelection`          | `BestAvailable` | Security policy selection                            |
| `opc:CertificateHandling`        | `AcceptAll`   | How server certificates are validated                  |
| `opc:AutoGenerateClientCertificate` | `true`     | Generate the client certificate automatically          |
| `opc:EnableTraceLog`             | `false`       | Enable OPC-UA stack trace logging                      |

### `OpcUaClient.Machines`

One entry per machine the client should talk to:

| Key                  | Example                                      | Description                                                       |
| :------------------- | :------------------------------------------- | :---------------------------------------------------------------- |
| `Name`               | `HermleC41`                                  | Logical machine name                                              |
| `opc:ServerAddress`  | `opc.tcp://localhost:4840/`                  | Address of the gateway's OPC-UA server                            |
| `opc:DncNodeId`      | `ns=2;s=HermleC41/Controller/NCPath1`        | NodeId of the machine's DNC command node (`Controller/NCPath1`)   |

The `opc:DncNodeId` points at the `Controller/NCPath1` node published by the gateway for that
machine; the client invokes the DNC methods (`OpenFileStream`, `ReadFileStream`, `ReadDirectory`,
`ReadToolRecord`, …) underneath it. To add a machine, append another entry to the `Machines`
array with the matching `ServerAddress` and `DncNodeId`.

## See Also

- [DNC Use Case](../Readme.md)
- [DNC Client Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.DncClient/)
- [HumanOS DNC Model](https://doc.cybertech.swiss/runtime/Models/HumanOS.DNC/)
</content>
