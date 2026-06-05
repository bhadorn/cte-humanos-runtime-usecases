# Heidenhain CSV Data Logger

In this use case the HumanOS® IoT Designer is used to log data from a Heidenhain CNC controller into a `.csv` file.

## Main Steps

1. Setup Connection to Heidenhain Simulator - Start the Heidenhain Simulator and make sure HeidenhainDNC is configured correctly
2. Create an IoT Project with DataLogger - Add a Heidenhain OEE device template and configure the `CsvDataLogger` plugin — set the output file to `C:\Temp\HeidenhainData.csv`. Then tag the data nodes `OEEMachineState`, `OEEMachineStateName`, `OEEProductName`, and `OEEProductionStep` with an `EnableCsv = true` property so the node filter picks them up.
3. Create Heidenhain Device & Verify - Deploy the Heidenhain device, start the IoT Gateway, run an NC program on the simulator, and open the CSV file to verify the logged data.

The project can be extended with `Alarm Event Logging`

- Add a second publisher (`AePublisher`) with a separate CSV file (`C:\Temp\HeidenhainAlarms.csv`), configure an alarm dataset with fields like `ConditionName`, `Alarm Message`, `TimeStamp`, etc., and tag the `MachineAlarming` node with `EnableCsv = true`. Trigger an alarm in the simulator and verify it appears in the alarm CSV.

## See Also

- [Heidenhain CSV data logger step by step manual](https://doc.cybertech.swiss/runtime/Tutorials/Setup/Tutorial.Setup.Heidenhain.md)
