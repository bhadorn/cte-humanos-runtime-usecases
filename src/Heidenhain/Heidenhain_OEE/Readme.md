# Heidenhain OEE

In this use case the HumanOS® IoT Designer is used to build a bridge between a Heidenhain CNC controller (here the iTNC530 simulator) and an OPC-UA server. This setup is then used to show how to calculate OEE (Overal Equipment Effectiveness) using a HumanOS® OEE template.

## Main Steps

1. Set up a connection to the Heidenhain Simulator - Start the simulator and use HeidenhainDNC to create a connection named `ItncSim` pointing to `localhost:19000`
2. Create a Designer project - Create an IoT project and define a device template for Heidenhain controllers. Inside, build a group structure (`Controller`) with data nodes: `RunningState`, `OperationMode`, `MachinePositions`, and `CurrentProgram`.
3. Deploy the Heidenhain device - Add the device to the default target, start the IoT Gateway, and connect via OPC-UA client at `opc.tcp://localhost:4840`.

   The bridge can be extended with:

   - History Mode: Enable historical recording for selected nodes (e.g. `OperationMode`) with a configurable sampling rate and retention time — viewable as a trend in UAExpert.
   - Commands: Execute actions on the controller via OPC-UA, e.g. `ReadFile` to read NC programs directly from the machine.
   - Alarm & Event Handling: A `MachineAlarming` node captures controller alarms (e.g. button presses in the simulator) and forwards them as OPC-UA events, visible in UAExpert's event view.

4. Prepare the Simulator - Select the NC program `TNC:\DEMO\CYCLES\drilling` and add two comment lines (`;PRODUCT: PLATE X` and `;STEP: DRILLING`) that the system will parse automatically.
5. Use OEE Templates - Add a Heidenhain OEE template in your project and deploy the IoT Gateway. Starting the NC program will shift the `MachineState` from `320 – Stopped Missing Personal` to `200 – Production`.
6. Extend the Logic - Add a custom state `201` (Drilling) to `MachineStateProcessor` using a predicate that checks `OperationMode`, `RunningState`, and the current program name.

## See Also

- [Heidenhain OEE step by step manual](https://doc.cybertech.swiss/runtime/Tutorials/Setup/Tutorial.Setup.Heidenhain.md)
