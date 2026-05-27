# SparkPlug Configuration for Alarming

Its possible to send alarms via Sparkplug, however, in the Sparkplug standard this is not done via a separate, special message type, but via process data (metrics).

## Processing Steps

1. Alarms are caught by the AlarmEvent Pool and the System Alarm task.
2. The `AlarmProcessor` processes all current alarms every 30 seconds
   - Alarms are the streamed to a json array and passed via output port to a data node
3. The spark plug gets informed when data node content changes and sends the alarm stream to the clients.

## See Also

- [SparkPlug Client Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.SparkPlugClient/)
