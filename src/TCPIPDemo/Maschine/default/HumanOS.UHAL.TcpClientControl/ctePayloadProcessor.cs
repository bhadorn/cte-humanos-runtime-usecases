/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2022 � www.cybertech.swiss         *
 *****************************************************************************
 * Project: HumanOS (R)
 * Date   : 14.6.2025
 *****************************************************************************
 * License:                                                                  *
 *   This library is protected software; you are not allowed to redistribute *
 *   whole or part of it to other companies or external persons without the  *
 *   authorization of the CEO CyberTech Engineering GmbH.                    *
 *****************************************************************************/

using HumanOS.Kernel;
using HumanOS.Kernel.Communication;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.InfoModel;
using HumanOS.Kernel.UHAL.InfoModel;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using System;
using System.Linq;

namespace HumanOS.UHAL.TcpClientControl
{
  /// <summary>
  /// Implements the cte Csv payload parsing.
  /// Connect to the machine using port 7872
  /// </summary>
  public class ctePayloadProcessor : TAbstractStreamScriptObject
  {
    // Constants od header
    private const string MachineId = "Name";
    private const string MachineState = "State";
    private const string ErrorId = "ErrCode";
    private const string ProgramName = "ProgramName";
    private const string StartDateTime = "StartDateTime";
    private const string ProductionTime = "ProductionTime";
    private const string OperationTimer = "OperationTimer";
    private const string PartCounter = "PartCounter";

    // Static header line
    private string[] m_astrCsvHeader = [MachineId,
                                        MachineState,
                                        ErrorId,
                                        ProgramName,
                                        StartDateTime,
                                        ProductionTime,
                                        OperationTimer,
                                        PartCounter];

    ///<see cref="TAbstractStreamScriptObject"/>
    public override void handleStream(IKernelAccess Kernel, TDeviceSchemaInfo DeviceInfo, IDataStream DataStream)
    {
      byte[] aui8Buffer = new byte[40000];
      DataStream.read(aui8Buffer, 0, 40000);
      string strText = System.Text.Encoding.ASCII.GetString(aui8Buffer).trimWhiteSpaces().Trim('\0');

      Logger.writeDebug($"Data '{strText}' was sent. {(int)strText[strText.Length - 1]}");

      // Parse CSV string
      // Split all lines
      string[] astrReceivedCsvLines = strText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

      // If two lines, the first one is the header
      string strRecord = astrReceivedCsvLines.Length > 1 ? astrReceivedCsvLines[1] : astrReceivedCsvLines[0];

      // Trim
      strRecord = strRecord.TrimEnd(new char[] { ';' }).Trim();

      // Split CSV to single items
      string[] astrCsvRecordItems = strRecord.Split(new char[] { ';' });

      // Check if items matches header count.
      if (astrCsvRecordItems.Length == m_astrCsvHeader.Length)
      {
        for (int i = 0; i < m_astrCsvHeader.Length; i++)
        {
          string strNodeName = m_astrCsvHeader[i];
          string strValue = astrCsvRecordItems[i].Trim('\"');

          switch (strNodeName)
          {
            case MachineId:
              strNodeName = "MachineId";
              break;
            case MachineState:
              strNodeName = "MachineState";
              break;
            case ProgramName:
              strNodeName = "OEEProductName";
              break;
            case ErrorId:
              strNodeName = "ErrorId";
              break;

            // e.g. hhhhh:mm
            case OperationTimer: // Fall through.
            // e.g. hh:mm:ss
            case ProductionTime:
              strValue = formatToMinutes(strValue); ;
              break;
            case StartDateTime: // Fall through.
            case PartCounter: // Fall through.
            default:
              break;
          }

          setValue(Kernel.NodeSpace, DeviceInfo, strNodeName, strValue);
        }
      }
      else
      {
        Logger.writeError($"Mismatch between item count and header count. Header count: {m_astrCsvHeader.Length}, Items count: {astrCsvRecordItems.Length}");
      }
    }

    /// <summary>
    /// sets a value to a data node
    /// </summary>
    /// <param name="NodeSpace">nodespace</param>
    /// <param name="DeviceInfo">device info model</param>
    /// <param name="strNodeName"></param>
    /// <param name="strValue"></param>
    private void setValue(INodeSpace NodeSpace, TDeviceSchemaInfo DeviceInfo, string strNodeName, string strValue)
    {
      TDataNodeInfo nDataNodeInfo = DeviceInfo.DataNodes.FirstOrDefault(n => n.Name == strNodeName);
      try
      {
        if (nDataNodeInfo != null)
        {
          if (NodeSpace.tryGetNode<IDataNode>(nDataNodeInfo.Id, out IDataNode DataNode))
          {
            Type tDataType = Type.GetType(nDataNodeInfo.DataType);
            TSimpleVariant Value = new TSimpleVariant(TValueConverter.convertToObject(tDataType, strValue));
            DataNode.passValue(Value, false, EDataState.Good);
          }
          else
          {
            Logger.writeWarning($"Node '{nDataNodeInfo.Id}' not found.");
          }
        }
      }
      catch (Exception Exc)
      {
        Logger.writeError($"Could not write data node '{strNodeName}'. {Exc.Message}");
      }
    }

    /// <summary>
    /// processes the operation time
    /// </summary>
    /// <param name="strInput">input string formated as hhhh:mm</param>
    /// <returns>total minutes as a string</returns>
    private string formatToMinutes(string strInput)
    {
      string strReturnTotalMinutes = "0";

      if (!string.IsNullOrWhiteSpace(strInput))
      {
        string[] astrParts = strInput.Split(':');

        int iHours = 0, iMinutes = 0, iSeconds = 0;
        bool bIsValid = false;

        //hh:mm:ss
        if (astrParts.Length == 3)
        {
          bIsValid = int.TryParse(astrParts[0], out iHours) &&
                     int.TryParse(astrParts[1], out iMinutes) &&
                     int.TryParse(astrParts[2], out iSeconds);
        }
        //hh:mm
        else if (astrParts.Length == 2)
        {
          bIsValid = int.TryParse(astrParts[0], out iHours) &&
                     int.TryParse(astrParts[1], out iMinutes);
        }

        if (bIsValid)
        {
          int iTotalMinutes = iHours * 60 + iMinutes + (iSeconds / 60);
          strReturnTotalMinutes = iTotalMinutes.ToString();
        }
        else
        {
          Logger.writeError($"Failed to read time value '{strInput}'.");
        }
      }

      return strReturnTotalMinutes;
    }
  }
}
