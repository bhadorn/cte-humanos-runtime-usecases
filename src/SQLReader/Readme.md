---
id: usecase-purchase-order-tracker
title: "Use Case: Purchase Order Tracker (SQL to MQTT)"
subject: "Polling active production orders from MySQL and publishing start/finish events to an MQTT broker with client-certificate authentication"
keywords: [HumanOS, SqlClient, MySQL, Sql.ReadEntities, TGenericEntity, MQTT, mutual TLS, client certificate, OnTimer, rule script, production orders]
---

# Purchase Order Tracker (SQL to MQTT)

Every 10 seconds this example reads the active production orders from a MySQL database, compares them with the result of the previous read, and sends new orders as **PoStart** and orders that have disappeared as **PoFinish** to an MQTT broker.

Built with HumanOS IoT Designer for Runtime **2.12**.

## Architecture

```
 Timer (10 s) ──► Rule script ProductionOrderMonitor.cs
                     │
                     ├─► Device "MES MySQL"   (HumanOS.UHAL.SqlClient)   Command ReadEntities = Sql.ReadEntities
                     │       SELECT … → active orders as TGenericEntity rows
                     │
                     ├─  Compare with the state from the previous tick
                     │
                     └─► Data nodes "PoStart" / "PoFinish" on the device "MES MySQL"
                             │
                             ▼
                         Publisher "PoPublisher" (HumanOS.PeSeL.MQTTClient, TLS + client certificate)
                             <Topic>/PoStart  = 12345
                             <Topic>/PoFinish = 12340
```

You need **1 device** (SQL), **1 rule script** triggered by a **timer**, and **1 publisher** in the `HumanOS.PeSeL.MQTTClient` plugin.
The script only writes the order number to the matching data node. Publishing and authenticating against the broker with a certificate is done by the publisher.

## Setup

### 1. Plugins in the target

- `HumanOS.UHAL.SqlClient`
- `HumanOS.PeSeL.MQTTClient` (with the payload script `PoPayload.cs`)
- (`HumanOS.UHAL.DeviceDetectors`, default)

Runtime version **2.12** is required (`Sql.ReadEntities` is available from this version on).

### 2. Device "MES MySQL" (SqlClient driver)

- **Address:** `mysql@Server=<IP>;Port=3306;SslMode=none;Database=<DB>;`
- **Property `SecretName`:** Name of a secret (type *UserPassword*) in the project's secret database holding the DB user. In the example it is provided through the variable `DB_SECRET` = `MesDbUser`. You have to create the secret itself in the Designer.
- **Property `Role` = `MesDB`:** Lets the script find the device without hard-coding the device id.
- **EntityType `active_order`:** One field `PoNumber` (`System.Int32`, key). It describes how an SQL row is converted into an entity. Its id (`07b56f22-21eb-46b5-b5c9-066abe08710e`) is also used in the script as `ActiveOrderTypeId` and must match. If you recreate the device in the Designer, check whether the EntityType id is still the same, otherwise update the script.
- **Command `ReadEntities`:** Address `Sql.ReadEntities`, arguments `QueryCommand` (In), `EntityTypeNodeId` (In) and `Data` (Out, `TGenericEntity[]`).
- **Data nodes `PoStart` and `PoFinish`:** `System.Int32`, TriggerType `OnEvent`, property `EnableMqtt` = `true`, **without Address and without Access**. The SqlClient driver only polls a data node if it has an address and `Access.Receive`, so it leaves these two nodes alone and only the script writes them. The script finds them by name and device, so their ids do not matter.

### 3. Publisher "PoPublisher" (plugin HumanOS.PeSeL.MQTTClient)

Configured in `default\HumanOS.PeSeL.MQTTClient\settings.json`:

- **`SamplingRate` = `0`:** The publisher is event-based. Every value the script writes to `PoStart` or `PoFinish` is published immediately as a separate message. The data nodes use `TriggerType` = `OnEvent` so that the same number is sent again even if it is written twice in a row.
- **DataSet:** Takes all data nodes with the property `EnableMqtt` = `true`, i.e. `PoStart` and `PoFinish`.
- **`ServiceRule`:** `Type` = `SelectedDevices` with the id of the device "MES MySQL" (`994b13b6-…`). If you recreate the device, enter its new id here.
- **`Topic`:** e.g. `company/line1`. `PoPayload.cs` appends the name of the data node, which results in the topics `company/line1/PoStart` and `company/line1/PoFinish`. The payload is the order number as plain text.
- **`ErrorHandlingPolicy` = `ReconnectLogger`:** If the broker is unreachable, the publisher reconnects. The default `ShutdownApplication` would stop the whole runtime.

**Certificate authentication (mutual TLS):**

| Property                        | Value in the example                                      | Meaning                                                                                       |
| ------------------------------- | --------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| `Address`                       | `192.168.0.20`                                            | Must match the name in the server certificate (CN/SAN), otherwise validation fails            |
| `ServerPort`                    | `8883`                                                    | TLS port of the broker                                                                        |
| `TransportLayerSecurity`        | `Tls12`                                                   | Enables TLS (`Tls13` if the broker supports it). Without TLS no client certificate is used    |
| `ClientCertificateFile`         | `$(AppConfig:CommonAppPath)Certificates\mqtt-client.pfx` | Client certificate **including the private key** as `.pfx`/`.p12`                             |
| `ClientCertificatePassword`     | `<PFX password>`                                          | Password of the `.pfx` file                                                                   |
| `ClientCaCertificateFile`       | empty                                                     | Optional: intermediate certificate sent to the broker together with the client certificate    |
| `IgnoreServerCertificateErrors` | `false`                                                   | Set to `true` for testing only                                                                |

If you have the certificate and key as `.crt` + `.key`, convert them like this:

```powershell
openssl pkcs12 -export -in client.crt -inkey client.key -out mqtt-client.pfx
```

The broker's **server certificate** is validated against the Windows certificate store. If it was issued by your own CA, import the CA certificate on the gateway machine under *Local Computer → Trusted Root Certification Authorities*.

If your broker additionally requires username/password, create a secret (type *UserPassword*) for it and enter its name in `ConnectionSecret`.

> If your existing setup already has a working `HumanOS.PeSeL.MQTTClient`, you can copy the values of `Address`, `ServerPort`, `TransportLayerSecurity` and the three `Client…Certificate…` properties directly from there.

### 4. Timer + rule

Create an object "Polling Timer" in the DataModel:

- Timer: Interval `10`, Unit `Second`
- Rule: TriggerEvent `OnTimer`, Action = ScriptFile `ProductionOrderMonitor.cs`

The script is located in `Scripts\Rules\` (not in the plugin folder).

### 5. Adapt the script

In `ProductionOrderMonitor.cs` you only need to adapt the query:

```csharp
private const string ACTIVE_ORDERS_QUERY =
  "SELECT `PO_NR` AS PoNumber FROM `production_orders` WHERE `Status` = 'ACTIVE';";
```

Important: the result column **must** have the same name as the field in the EntityType, hence `AS PoNumber`.
If the column in MySQL is a `BIGINT`, change the field in the EntityType, both data nodes and the field `Value` in the DataSet (`settings.json`) to `System.Int64`. In the script replace `int` with `long` (`getPo()`, `containsPo()`, `getDataNode()`), and in `PoPayload.cs` replace `getFieldValue<int>` with `getFieldValue<long>`.

## Script Behavior

- **Previous state:** The script keeps the rows of the last `ReadEntities` directly as `IEnumerable<TGenericEntity>` (`s_PreviousOrders`). Rows are compared by the key field `PoNumber` (`getPo()`), because every `ReadEntities` returns new entity objects. If you later need more columns in the MQTT payload (e.g. article or quantity), add them to the EntityType and the query. They are then also available for PoFinish, because the old row is still stored.
- **First read after startup:** The script only stores the current state and sends nothing. Otherwise every runtime restart would send a PoStart for all running orders.
- **DB unreachable / query error:** The tick is skipped and the previous state is kept. This prevents false PoFinish messages when the DB is briefly unavailable.
- **Payload:** The order number as plain text, e.g. `12345`.
- **Limitation (broker outage):** Messages produced while the broker is unreachable are lost. The publisher can only buffer them if the data nodes are historized (see manual *Historization*); this project runs with the in-memory NodeSpace without historization.
- **Limitation (state in memory):** The comparison state is held in memory only. If an order starts or ends while the runtime is stopped, no message is generated for it.

## Files

```
PurchaseOrderTracker.h2proj
DeviceTemplates\MES MySQL.json
default\Devices\MES MySQL.json
default\DataModel\Objects\PollingTimer.json
default\Scripts\Rules\ProductionOrderMonitor.cs
default\HumanOS.UHAL.SqlClient\settings.json
default\HumanOS.PeSeL.MQTTClient\settings.json
default\HumanOS.PeSeL.MQTTClient\PoPayload.cs
default\HumanOS.UHAL.DeviceDetectors\settings.json
```

**Adapt before publishing:**

- IP addresses, DB name and topic
- Create the secret `MesDbUser`
- Copy `mqtt-client.pfx` on the gateway to `<CommonAppPath>\Certificates\` and set `ClientCertificatePassword`
- Set the `ProductLicenseId` in the project and on the SQL device to your license (currently placeholder `0000…`). The device id must match the device license.

## Documentation

- SQL Commands: https://doc.cybertech.swiss/runtime/2.12/Manuals/HumanOS.UHAL.SqlClient/Commands
- MQTT Client: manual `HumanOS.PeSeL.MQTTClient` (Publisher Configuration; Service Configuration → Note on Certificates)
