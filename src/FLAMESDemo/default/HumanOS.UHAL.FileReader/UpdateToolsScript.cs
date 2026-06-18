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

using CyberTech;
using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Entity;
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace HumanOS.IoT.Designer.Library.Scripts;

/// <summary>
/// Processing script for update tools processor
/// </summary>
public class TUpdateTools : TAbstractProcessingScriptObject
{
  ///<see cref="TAbstractProcessingScriptObject"/>
  public override void process(IProcessingNode Processor)
  {
    Logger.writeInfo($"TUpdateTools processor started...");

    // read file (json)
    // get all loaded files as json string ()
    // foreach tool
    // if exists and changed -> update (optional)
    // else create new -> call command 
    // delete old tools not in file


    IGroupRelation? nDeviceNode = Processor.NodeSpace.queryNodeLocally(n => n.Name == "FLAMES") as IGroupRelation;
    if (nDeviceNode != null)
    {
      IGroupRelation? nMachineNode = Processor.NodeSpace.queryNodeLocally(n => n.Name == "Machine") as IGroupRelation;
      if (nMachineNode != null)
      {
        IGroupRelation? nToolDataManagementNode = Processor.NodeSpace.queryNodeLocally(n => n.Name == "ToolDataManagement") as IGroupRelation;
        if (nToolDataManagementNode != null)
        {
          var existingTools = nToolDataManagementNode.getAllNodes().Where(n => n.NodeType.Name == "TGroupRelation").ToDictionary(d => d.Name, d => d);
          

          // read file content and parse json
          string jsonString = File.ReadAllText(@"C:\Projects\cte-humanos-runtime\Demo\FLAMESDemo\Machine.json");
          JObject jData = JObject.Parse(jsonString);

          JObject toolManagement = jData["Flames"]?["ToolDataManagement"]?["Tools"] as JObject;
          if (toolManagement != null)
          {
            var jsonToolNames = new HashSet<string>();

            foreach (var toolProp in toolManagement.Properties())
            {
              string toolName = toolProp.Name;
              jsonToolNames.Add(toolName);

              if (!existingTools.ContainsKey(toolName))
              {
                Logger.writeInfo($"Creating tool '{toolName}'");

                TCommandArgs args = new TCommandArgs();

                args.Input["toolName"] = toolName;
                //args.Output["toolNodeId"] = Guid.Empty;

                TCommandResult result = TCommandHelper.call(nToolDataManagementNode, "CreateTool_Internal", args);

                if (result.State != EProcessingState.Good)
                {
                  throw new ArgumentException($"Command 'CreateTool_Internal' failed. {result.ErrorMessage}");
                }
              }
            }

            foreach (var existing in existingTools)
            {
              if (!jsonToolNames.Contains(existing.Key))
              {
                Logger.writeInfo($"Deleting tool '{existing.Key}'");

                TCommandArgs args = new TCommandArgs();

                args.Input["objectToDelete"] = existing.Value.GlobalId;
                args.Output["deleted"] = false;

                TCommandResult result = TCommandHelper.call(nToolDataManagementNode, "Delete_Internal", args);

                if (result.State != EProcessingState.Good)
                {
                  throw new ArgumentException($"Command 'Delete_Internal' failed. {result.ErrorMessage}");
                }
              }
            }
          }
          else
          {
            throw new ArgumentException("ToolDataManagement not found in json.");
          }
        }
        else
        {
          throw new ArgumentException($"No Group 'ToolDataManagement' not found.");
        }
      }
      else
      {
        throw new ArgumentException($"No Group 'Machine' not found.");
      }
    }
    else
    {
      throw new ArgumentException($"No Group 'FLAMES' not found.");
    }

    // Output ----------------------------------------------------
    //Processor.setProperty("Output", EntityToolLoadedEvent);
    Guid DeviceId = Processor.getProperty<Guid>("DeviceId");
    Logger.writeInfo("TUpdateTools finished...");
  }
}
