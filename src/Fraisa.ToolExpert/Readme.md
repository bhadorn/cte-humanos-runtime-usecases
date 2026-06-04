# Fraisa ToolExpert

This use case shows how to integrate the [ToolExpert](https://toolexpert.fraisa.com) from FRAISA.

1. The API of the FRAISA ToolExpert is accessed using the [WebControl connector](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.WebControl/)
2. The tool is search by article number, e.g. `P527930`
3. The result is a JSON formatted string which is parsed and returned as content

The example uses [OPC-UA server](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/) as service interface.
