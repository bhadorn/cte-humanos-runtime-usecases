# Tutorial: Coffee Report

A HumanOS IoT Gateway tutorial project demonstrating JSON device integration, custom C# data processing, and dual-export (OPC-UA + SQL) for a coffee machine scenario.

## What this tutorial covers

- Reading device data from a JSON file using the FileReader driver
- Defining a device template with raw and processed data groups
- Writing a custom C# processing script to transform raw values into analytics
- Logging processed data to a MySQL database via the SQL Data Logger
- Exposing live data over OPC-UA

## Step by Step Guide

In [Coffee Report Step by Step Guide](https://doc.cybertech.swiss/runtime/Tutorials/Tutorial6/04_Example.md) you can find a step-by-step instruction of this tutorial.

## Architecture overview

```text
coffee.json (file)
      │
      ▼
 FileReader driver
      │  Raw strings (brand, amounts, strength, …)
      ▼
 CoffeeProcessor (C# script)
      │  Numeric outputs (ml, %, g)
      ▼
 Data Aggregator (500 ms buffer)
      │
      ├──► OPC-UA Server (port 4840)
      └──► MySQL Data Logger
```

## Data flow

### Raw data (read from JSON file)

| Node             | Type   | JSON path            |
| ---------------- | ------ | -------------------- |
| Brand            | String | `$.brand`            |
| Name             | String | `$.name`             |
| CoffeeAmount     | String | `$.coffeeamount`     |
| MilkAmount       | String | `$.milkamount`       |
| Strength         | String | `$.strength`         |
| CapsuleNetWeight | String | `$.capsulenetweight` |

### Processed data (output of CoffeeProcessor script)

| Node             | Type   | Unit | Description                           |
| ---------------- | ------ | ---- | ------------------------------------- |
| CoffeeAmount     | Double | ml   | Coffee volume (×2 conversion applied) |
| MilkAmount       | Double | ml   | Milk volume (×2 conversion applied)   |
| CoffeePercent    | Double | %    | Coffee share of total liquid          |
| MilkPercent      | Double | %    | Milk share of total liquid            |
| StrengthPercent  | Double | %    | Strength as percentage (X/Y × 100)    |
| CapsuleNetWeight | Double | g    | Capsule weight (÷1000 then ×2)        |

## Prerequisites

| Requirement     | Detail                                                               |
| --------------- | -------------------------------------------------------------------- |
| HumanOS Runtime | win-x86 TRIAL, version 2.6                                           |
| Input file      | `C:\Temp\CoffeeProject\coffee.json`                                  |
| MySQL           | localhost, database `humanos.datalogger`, user `humanos` / `humanos` |
