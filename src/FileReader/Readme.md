---
id: usecase-file-reader-excel
title: "Use Case: Excel-Reader (FileReader)"
subject: "Reading an Excel file with the FileReader and publishing its cells through the OPC-UA server"
keywords: [HumanOS, FileReader, Excel, xlsx, OPC-UA, data node, device template]
---

# Excel-Reader (FileReader)

Shows how the HumanOS **FileReader** reads an Excel file (`xlsx:` addresses) and how the **OPC-UA server** of the gateway makes the cells visible to any OPC-UA client. The gateway watches the file: a cell that you change and save in Excel shows up in the OPC-UA value within a few seconds, without a restart.

The project is called **FileReader** and holds one target per file format. The target **`Excel-Reader`** is described here; its device comes from the device template `EXCEL-READER`. The target `default` is not part of this example.

## Architecture

```text
MaintenanceOrders.xlsx  (edited by hand, e.g. in Excel)
        │  FileReader, addresses xlsx:$.Tabelle1[0].<column>  (read only)
        ▼
HumanOS gateway ── device ExcelReader ── data nodes Auftrag, Kurztext, ... and SheetData
        │
        ▼
OPC-UA server plugin ── opc.tcp://<host>:4840/  ──►  any OPC-UA client (e.g. UaExpert)
```

## Prerequisites

- A HumanOS IoT Runtime with Excel support in the FileReader: version **2.12.0.6863 or newer**. The trial runtime is enough for this example, it stops one hour after it was started
- HumanOS IoT Designer (or the command line `HumanOS.BuildTool`) to build the target
- Microsoft Excel to change the demo file
- An OPC-UA client to look at the result, for example UaExpert

## Key Concepts

- **One fixed file.** The FileReader watches exactly one file name (`MaintenanceOrders.xlsx`, next to `FileReader.h2proj`). The file must exist when the gateway starts. It does not watch a folder for new or dated files.
- **Read only.** Excel addresses are read-only; the gateway never changes the workbook.
- **Address.** `xlsx:$.<sheet>[<row>].<column>`: the first row of the sheet holds the column names, `row` is the 0-based index of a data row below it. A column name with a space (`Auftrag Bezeichnung`) is written as it is. The address `xlsx:$.Tabelle1` without row and column returns the whole sheet as a JSON array.
- **Values are text.** Numbers arrive as plain digit text, dates as ISO 8601 text (`2026-09-05T00:00:00.0000000`), booleans as `True` or `False`. A repeated column name gets the suffix `_2` (the file has two columns `Bezeichnung`).
- **Relative path.** The device address `..\..\..\MaintenanceOrders.xlsx` is resolved against the data folder of the built gateway (`Build\Excel-Reader\Data`), which leads back to the project folder. This holds for a gateway started from the built folder as described below; for a gateway installed somewhere else set the device address to the absolute path of the Excel file.
- **Receive.** The data nodes are configured with `Receive`, so the gateway updates them when the file changes. The OPC-UA server publishes the data nodes of a device as variables.

## Project Contents

| Path                                                   | Purpose                                                          |
| :----------------------------------------------------- | :--------------------------------------------------------------- |
| `FileReader.h2proj`                                    | Designer project, targets `default` and `Excel-Reader`           |
| `MaintenanceOrders.xlsx`                               | Demo data: 40 rows, sheet `Tabelle1`                             |
| `DeviceTemplates/EXCEL-READER.json`                    | Device template with the data nodes                              |
| `Excel-Reader/Devices/ExcelReader.json`                | Device `ExcelReader` of the target, created from the template    |
| `default/HumanOS.PeSeL.OPCUAServer/settings.json`      | Settings of the OPC-UA server plugin (port 4840); the target `Excel-Reader` uses them too |

The Excel columns are: `Auftrag`, `Auftrag Bezeichnung`, `Kurztext`, `Personalnr`, `Istarbeit`, `Einheit Arbeit`, `VrgArbeitsplatz`, `Auftragsart`, `Erfassungsdatum`, `Start Uhrzeit`, `Vorgang`, `Langtext vorh.`, `Priorität`, `Techn. Platz`, `Bezeichnung`, `Equipment`, `Bezeichnung`, `Kostenstelle`, `Erfasser`. The device publishes the first data row (row 0) of these columns and the whole sheet:

| Data node            | Address                                    | Value of the demo file                  |
| :------------------- | :----------------------------------------- | :-------------------------------------- |
| `Auftrag`            | `xlsx:$.Tabelle1[0].Auftrag`               | `9100001`                               |
| `AuftragBezeichnung` | `xlsx:$.Tabelle1[0].Auftrag Bezeichnung`   | `Demo maintenance order 1`              |
| `Kurztext`           | `xlsx:$.Tabelle1[0].Kurztext`              | `Visual inspection`                     |
| `Vorgang`            | `xlsx:$.Tabelle1[0].Vorgang`               | `0010`                                  |
| `Equipment`          | `xlsx:$.Tabelle1[0].Equipment`             | `EQ-DEMO-01`                            |
| `Erfassungsdatum`    | `xlsx:$.Tabelle1[0].Erfassungsdatum`       | `2026-09-05T00:00:00.0000000`           |
| `Istarbeit`          | `xlsx:$.Tabelle1[0].Istarbeit`             | `0.5`                                   |
| `SheetData`          | `xlsx:$.Tabelle1`                          | JSON array with the 40 rows             |

The columns `Auftrag`, `Personalnr`, `Vorgang` and `Equipment` are formatted as text in the workbook: keep them text, because a number typed into a general cell loses its leading zeros (`0010` becomes `10`).

## Build and Run

1. Open `FileReader.h2proj` in the Designer and build the target `Excel-Reader`. The target uses the trial runtime (`HumanOS.IoT.Runtime win-x86 TRIAL`), which stops one hour after it was started; choose a licensed runtime in the target settings if you have a license. On the command line (the trial runtime, or a licensed runtime with the license imported):

   ```text
   HumanOS.BuildTool.exe "FileReader.h2proj" --build --target=Excel-Reader
   ```

2. Start the gateway (`HumanOS.IoT.Gateway.exe` in the folder of the installed runtime) with the built folder:

   ```text
   HumanOS.IoT.Gateway.exe -AppSettingsDir="<project folder>\Build\Excel-Reader"
   ```

   About ten seconds after the start the log shows `Device [ExcelReader@..\..\..\MaintenanceOrders.xlsx] detected.`, `File watcher started on file ...MaintenanceOrders.xlsx` and `OpcUaServer: Server is running`. The OPC-UA server plugin listens on port 4840; switch it off in the target if another gateway on the same machine already uses that port.

3. Connect an OPC-UA client to `opc.tcp://<host>:4840/` (the log lists the endpoints; the example server allows an anonymous login and the security mode `None`). The device is at `Objects/Server/HumanOS/ExcelReader`; its variables are the data nodes of the table above (the server adds a variable `HistoricalEventFilter` that you can ignore).

## Demo

1. Look at the variables of `ExcelReader`: `Auftrag` is `9100001`, `Kurztext` is `Visual inspection`, `SheetData` holds the 40 rows.
2. Change the cell `Kurztext` of the first data row (row 2 of the sheet) in Excel and save the file; you can keep it open. Within a few seconds the variable `Kurztext` shows the new text and `SheetData` holds it in its first row.
3. Change `Erfassungsdatum` of that row: the variable shows the date as ISO text.

## Add More Cells

A data node reads one cell. To publish another cell, add a data node to the device (in the Designer: open the device `ExcelReader` or the device template `EXCEL-READER`, add a data node) with `Receive` enabled and the address `xlsx:$.Tabelle1[<row>].<column>`.

## Limits

- One fixed file name, existing at gateway start. A file that is saved under a new name is not noticed.
- Only the cells with a data node, and the whole sheet through `SheetData`, are published. `SheetData` is one text value; a large sheet makes a large value.
- Excel addresses are read-only.
