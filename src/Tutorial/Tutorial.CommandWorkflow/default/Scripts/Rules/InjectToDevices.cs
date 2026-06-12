/*****************************************************************************
* Copyright (C) by CyberTech Engineering 2022 – www.cybertech.swiss         *
*****************************************************************************
* Project: HumanOS (R)
* Date   : 2022
*****************************************************************************
* License:                                                                  *
*   This library is protected software; you are not allowed to redistribute *
*   whole or part of it to other companies or external persons without the  *
*   authorization of the CEO CyberTech Engineering GmbH.                    *
*****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Rules;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of a script for rules
  /// </summary>
  public class BlankPeMiLRuleScriptObject : TAbstractRuleScriptObject
  {
     ///<see cref="TAbstractRuleScriptObject"/>
     public override void execute(IKernelAccess Kernel, IVariablePool Pool)
     {
        IGroupRelation nDevice = null;
        Guid WorkflowSchemaId = Guid.Parse("A7FAA92A-99FC-4D38-BC68-E4915F333505"); //Id of injection schema
        try
        {
          nDevice = Pool.getValue<IGroupRelation>("Node");
          if (nDevice == null)
          {
            throw new ArgumentException($"Device not found, cannot map workflow schema to empty device.");
          }
          
          IEnumerable<INode> SchemaNodes = Kernel.NodeFactory.createNodesFromSchema(WorkflowSchemaId, nDevice.GlobalId);
          if (SchemaNodes.Count() == 0)
          {
            Logger.writeWarning($"Schema with id '{WorkflowSchemaId}' returned no nodes, nothing will be injected.");
          }
          
          foreach (INode SchemaNode in SchemaNodes)
          {
            try
            {
                Kernel.NodeSpace.addNodeToGroup(nDevice.GlobalId, SchemaNode.GlobalId);
            }
            catch (Exception Exc)
            {
                Logger.writeError($"Failed to add the node '{SchemaNode.Name}' to device '{nDevice.Name}'.", Exc);
            }
          }
          
          Logger.writeInfo($"Injection done successfully for device '{nDevice.Name}'.");
        }
        catch (Exception Exc)
        {
          Logger.writeError($"Failed to add the workflow schema with id '{WorkflowSchemaId}' to the device node '{nDevice?.Name}'.", Exc);
        }
     }         
  }
}