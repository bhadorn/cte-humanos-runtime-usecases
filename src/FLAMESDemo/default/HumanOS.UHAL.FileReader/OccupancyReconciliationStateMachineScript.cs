/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2024 – www.cybertech.swiss         *
 *****************************************************************************
 * Project: HumanOS (R)
 * Date   : 2026
 *****************************************************************************
 * License:                                                                  *
 *   This library is protected software; you are not allowed to redistribute *
 *   whole or part of it to other companies or external persons without the  *
 *   authorization of the CEO CyberTech Engineering GmbH.                    *
 *****************************************************************************/

using HumanOS.Kernel.InfoModel.StateMachine;
using HumanOS.Kernel.Processing.StateMachine;
using System.Threading.Tasks;

namespace HumanOS.Kernel.Test.TestFiles.Scripts.Processing;

/// <summary>
/// Occupancy reconciliation state machine
/// </summary>
public class TOccupancyReconciliationStateMachine : TAbstractStateMachineScript
{
  #region Implementation of TAbstractStateMachineScript

  ///<see cref="TAbstractStateMachineScript"/>
  public override async Task onEnterStateAsync(TTriggerEventInfo Trigger, TStateInfo Source, TStateInfo Destination, TStateMachineContext Context)
  {
    Logger.writeInfo($"TOccupancyReconciliationStateMachine: State '{Destination.Name}' entered.");
    await Task.CompletedTask.ConfigureAwait(false);
  }

  ///<see cref="TAbstractStateMachineScript"/>
  public override async Task onExitStateAsync(TTriggerEventInfo Trigger, TStateInfo Source, TStateInfo Destination, TStateMachineContext Context)
  {
    Logger.writeInfo($"TOccupancyReconciliationStateMachine: State '{Source.Name}' exit.");
    await Task.CompletedTask.ConfigureAwait(false);
  }

  ///<see cref="TAbstractStateMachineScript"/>
  public override async Task onInternalTransitionAsync(TTriggerEventInfo Trigger, TStateInfo Source, TStateMachineContext Context)
  {
    Logger.writeInfo($"TOccupancyReconciliationStateMachine: Internal transition from state '{Source.Name}'.");
    await Task.CompletedTask.ConfigureAwait(false);
  }

  #endregion Implementation of TAbstractStateMachineScript
}
