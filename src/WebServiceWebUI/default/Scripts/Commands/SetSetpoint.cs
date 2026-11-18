/*****************************************************************************
* Copyright (C) by CyberTech Engineering 2026 – www.cybertech.swiss         *
*****************************************************************************
* Project: HumanOS (R)
* Date   : 2026
*****************************************************************************
* License:                                                                  *
*   This library is protected software; you are not allowed to redistribute *
*   whole or part of it to other companies or external persons without the  *
*   authorization of the CEO CyberTech Engineering GmbH.                    *
*****************************************************************************/

using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Script;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Writes a new setpoint to the simulated machine (the FileReader writes it into the JSON file) and returns a
  /// confirmation with the old and the new value
  /// </summary>
  public class TSetSetpointCommand : TAbstractCommandScriptObject2
  {
    /// <summary>
    /// Lowest accepted setpoint in °C
    /// </summary>
    private const double MinSetpoint = 5.0;

    /// <summary>
    /// Highest accepted setpoint in °C
    /// </summary>
    private const double MaxSetpoint = 40.0;

    ///<see cref="TAbstractCommandScriptObject2"/>
    public override async Task<TCommandResult> executeCommandAsync(ICommandCallContext CallContext, CancellationToken Token)
    {
      TCommandResult Retval = new TCommandResult();
      try
      {
        double fSetpoint = CallContext.getInputArgumentValue<double>("Setpoint");
        if (double.IsNaN(fSetpoint) || fSetpoint < MinSetpoint || fSetpoint > MaxSetpoint)
        {
          Retval.setErrorInfo(EProcessingState.BadArguments,
                              new ArgumentException($"The setpoint must be between {MinSetpoint} and {MaxSetpoint} °C."));
        }
        else
        {
          IDataNode<double> SetpointNode = getDataNode(CallContext, "Machine", "Setpoint");
          double fOldSetpoint = SetpointNode.Value;

          //bInvokeWriteCommand = true: the FileReader writes the value into the JSON file
          SetpointNode.passValue(fSetpoint, true);
          CallContext.setOutputArgumentValue("Result", $"Setpoint changed from {format(fOldSetpoint)} °C to {format(fSetpoint)} °C.");
          Logger.writeInfo($"SetSetpoint: {format(fOldSetpoint)} -> {format(fSetpoint)} °C");
        }
      }
      catch (Exception Exc)
      {
        Retval.setErrorInfo(EProcessingState.BadProcessing, Exc);
      }
      return await Task.FromResult(Retval).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a data node of a device
    /// </summary>
    /// <param name="CallContext">context of the command call</param>
    /// <param name="strDeviceName">name of the device</param>
    /// <param name="strNodeName">name of the data node</param>
    /// <returns>data node</returns>
    /// <exception cref="InvalidOperationException">device or data node not found</exception>
    private static IDataNode<double> getDataNode(ICommandCallContext CallContext, string strDeviceName, string strNodeName)
    {
      INodeSpace NodeSpace = (INodeSpace)CallContext.CallingNode.Relations.First(n => n is INodeSpace);
      IGroupRelation nDevice = NodeSpace.queryNodesLocally(n => n.Name == strDeviceName).OfType<IGroupRelation>().FirstOrDefault();
      IDataNode<double> nDataNode = nDevice?.queryNodeLocally(n => n.Name == strNodeName) as IDataNode<double>;
      if (nDataNode == null)
      {
        throw new InvalidOperationException($"Data node '{strDeviceName}.{strNodeName}' not found.");
      }
      return nDataNode;
    }

    /// <summary>
    /// Formats a temperature with one decimal
    /// </summary>
    /// <param name="fValue">value in °C</param>
    /// <returns>formatted value</returns>
    private static string format(double fValue)
    {
      return fValue.ToString("0.0", CultureInfo.InvariantCulture);
    }
  }
}
