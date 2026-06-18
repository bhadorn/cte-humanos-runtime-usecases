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

using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.DataModel.Entity;
using HumanOS.Kernel.DataModel.Space;
using HumanOS.Kernel.InfoModel;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HumanOS.IoT.Designer.Library.Scripts;

/// <summary>
/// Example for FileSystemCreateDirectory
/// </summary>
public class TFileSystemCreateDirectory : TAbstractLogicScriptObject
{
  private const string strBaseDirectoryPath = "C:\\Temp\\Flames\\FileSystem";

  ///<see cref="TAbstractLogicScriptObject"/>
  public override async Task executeCommandAsync(IKernelAccess Kernel, IGroupRelation DeviceNode, ICommandCallContext CallContext, CancellationToken Token)
  {
    switch (CallContext.CallingNode.Name)
    {
      case "CreateDirectory":
        await createDirectoryAsync(Kernel, DeviceNode, CallContext, Token).ConfigureAwait(false);
        break;

      case "Delete":
      case "Delete_Internal": // fall through
        await deleteAsync(Kernel, DeviceNode, CallContext, Token).ConfigureAwait(false);
        break;

      case "CreateFile":
        await createFileAsync(Kernel, DeviceNode, CallContext, Token).ConfigureAwait(false);
        break;

      case "MoveOrCopy":
        await moveOrCopyAsync(Kernel, DeviceNode, CallContext, Token).ConfigureAwait(false);
        break;

      case "CreateTool_Internal":
        await createToolAsync(Kernel, DeviceNode, CallContext, Token).ConfigureAwait(false);
        break;

      case "CreateToolPosition_Internal":
        await createToolPositionAsync(Kernel, DeviceNode, CallContext, Token).ConfigureAwait(false);
        break;

      case "CreatePosition_Internal":
        await createPositionAsync(Kernel, DeviceNode, CallContext, Token).ConfigureAwait(false);
        break;

      // TODO File commands when needed
      case "Open":
      case "Close":
      case "Read":
      case "Write":
      case "GetPosition":
      case "SetPosition":
      default:
        throw new ArgumentException($"Invalid command '{CallContext.CallingNode.Name}'.");
    }
  }

  /// <summary>
  /// Creates a position
  /// </summary>
  private async Task createPositionAsync(IKernelAccess Kernel, IGroupRelation Device, ICommandCallContext CallContext, CancellationToken Token)
  {
    string strPositionName = CallContext.getInputArgumentValue<string>("positionName");
    IGroupRelation? nBaseDirectory = CallContext.CallingNode.Relations.FirstOrDefault(n => n is IGroupRelation && n is not INodeSpace) as IGroupRelation;

    if (nBaseDirectory != null)
    {
      IGroupRelation FileGroup = createPositionInternal(Kernel, Device, strPositionName, nBaseDirectory);

      CallContext.setOutputArgumentValue("positionNodeId", FileGroup.GlobalId);
      Logger.writeInfo($"New tool position '{strPositionName}' created.");
    }
    else
    {
      Logger.writeError($"Creating tool position '{strPositionName}' failed. Could not find the base directory.");
    }
    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>
  /// Creates a position without call context
  /// </summary>
  private IGroupRelation createPositionInternal(IKernelAccess Kernel, IGroupRelation Device, string strPositionName, IGroupRelation BaseDirectory)
  {
    string strBasePath = BaseDirectory.getProperty<string>("opc-ua:flames:Mapping") + $"/{strPositionName}";

    IGroupRelation PositionGroup = createGroupWithMapping(Kernel, strPositionName, strBasePath, "FacilityComponentPositionType");

    // Occupied, UniqueId, SetIndicatorMode
    createBasePosition(Kernel, Device, strBasePath, PositionGroup);

    // Add tool position node to parent
    Kernel.NodeSpace.addNodeToGroup(BaseDirectory.GlobalId, PositionGroup.GlobalId);
    return PositionGroup;
  }

  /// <summary>
  /// Creates the base position type
  /// </summary>
  private void createBasePosition(IKernelAccess Kernel, IGroupRelation Device, string strBasePath, IGroupRelation ToolPositionGroup)
  {
    // Occupied
    addDataNode(Kernel, ToolPositionGroup.GlobalId, "Occupied", typeof(bool), strBasePath + "/Occupied");

    // UniqueId
    addDataNode(Kernel, ToolPositionGroup.GlobalId, "UniqueId", typeof(string), strBasePath + "/UniqueId");

    DAsyncCommandCall dAction = createDefaultCommandAction(Kernel, Device);

    // SetIndicatorMode
    addCommand(Kernel, ToolPositionGroup.GlobalId, "SetIndicatorMode", strBasePath + "/SetIndicatorMode", dAction, c =>
    {
      c.addOrUpdateArgument<bool>("mode", EArgumentType.Input, "");
      c.addOrUpdateArgument<TGenericEntity[]>("applicationData", EArgumentType.Input, "");
    });
  }

  /// <summary>
  /// Creates a tool position
  /// </summary>
  private async Task createToolPositionAsync(IKernelAccess Kernel, IGroupRelation Device, ICommandCallContext CallContext, CancellationToken Token)
  {
    string strToolPositionName = CallContext.getInputArgumentValue<string>("toolPositionName");
    IGroupRelation? nBaseDirectory = CallContext.CallingNode.Relations.FirstOrDefault(n => n is IGroupRelation && n is not INodeSpace) as IGroupRelation;

    if (nBaseDirectory != null)
    {
      IGroupRelation FileGroup = createToolPositionInternal(Kernel, Device, strToolPositionName, nBaseDirectory);

      CallContext.setOutputArgumentValue("toolPositionNodeId", FileGroup.GlobalId);
      Logger.writeInfo($"New tool position '{strToolPositionName}' created.");
    }
    else
    {
      Logger.writeError($"Creating tool position '{strToolPositionName}' failed. Could not find the base directory.");
    }
    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>
  /// Creates a tool position without call context
  /// </summary>
  private IGroupRelation createToolPositionInternal(IKernelAccess Kernel, IGroupRelation Device, string strToolPositionName, IGroupRelation BaseDirectory)
  {
    string strBasePath = BaseDirectory.getProperty<string>("opc-ua:flames:Mapping") + $"/{strToolPositionName}";

    IGroupRelation ToolPositionGroup = createGroupWithMapping(Kernel, strToolPositionName, strBasePath, "ToolPositionType");

    // Occupied, UniqueId, SetIndicatorMode
    createBasePosition(Kernel, Device, strBasePath, ToolPositionGroup);

    // OccupyingToolNodeId
    addDataNode(Kernel, ToolPositionGroup.GlobalId, "OccupyingToolNodeId", typeof(Guid), strBasePath + "/OccupyingToolNodeId");

    // add tool position node to parent
    Kernel.NodeSpace.addNodeToGroup(BaseDirectory.GlobalId, ToolPositionGroup.GlobalId);
    return ToolPositionGroup;
  }

  /// <summary>
  /// Creates a tool
  /// </summary>
  private async Task createToolAsync(IKernelAccess Kernel, IGroupRelation Device, ICommandCallContext CallContext, CancellationToken Token)
  {
    string strToolName = CallContext.getInputArgumentValue<string>("toolName");
    IGroupRelation? nBaseDirectory = CallContext.CallingNode.Relations.FirstOrDefault(n => n is IGroupRelation && n is not INodeSpace) as IGroupRelation;

    if (nBaseDirectory != null)
    {
      IGroupRelation FileGroup = createToolInternal(Kernel, Device, strToolName, nBaseDirectory);

      CallContext.setOutputArgumentValue("toolNodeId", FileGroup.GlobalId);
      Logger.writeInfo($"New tool '{strToolName}' created.");
    }
    else
    {
      Logger.writeError($"Creating tool '{strToolName}' failed. Could not find the base directory.");
    }
    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>
  /// Creates a tool without call context
  /// </summary>
  private IGroupRelation createToolInternal(IKernelAccess Kernel, IGroupRelation Device, string strToolName, IGroupRelation BaseDirectory)
  {
    //INFO: It's not possible to link node to device dynamically. Query the node inside the script and set its value with .passValue()
    string strBasePath = BaseDirectory.getProperty<string>("opc-ua:flames:Mapping") + $"/{strToolName}";

    IGroupRelation ToolGroup = createGroupWithMapping(Kernel, strToolName, strBasePath, "ToolType");

    // Identifier
    addDataNode(Kernel, ToolGroup.GlobalId, "Identifier", typeof(TGenericEntity), strBasePath + "/Identifier", "ToolIdentifierType", "ToolIdentifier");
    addDataNode(Kernel, ToolGroup.GlobalId, "Identifier.Name", typeof(string), strBasePath + "/Identifier/Name");
    addDataNode(Kernel, ToolGroup.GlobalId, "Identifier.Duplonumber", typeof(uint), strBasePath + "/Identifier/Duplonumber");

    // Locked
    addDataNode(Kernel, ToolGroup.GlobalId, "Locked", typeof(bool), strBasePath + "/Locked");

    // NativeDataset
    addDataNode(Kernel, ToolGroup.GlobalId, "NativeDataset", typeof(string), strBasePath + "/NativeDataset");

    // ToolLife
    addDataNode(Kernel, ToolGroup.GlobalId, "ToolLife", typeof(uint), strBasePath + "/ToolLife");
    addDataNode(Kernel, ToolGroup.GlobalId, "ToolLife.EngineeringUnits", typeof(TGenericEntity), strBasePath + "/ToolLife/EngineeringUnits", "", "0,EUInformation");

    // UnloadMarker
    addDataNode(Kernel, ToolGroup.GlobalId, "UnloadMarker", typeof(bool), strBasePath + "/UnloadMarker");

    DAsyncCommandCall dAction = createDefaultCommandAction(Kernel, Device);

    // MarkForUnloading
    addCommand(Kernel, ToolGroup.GlobalId, "MarkForUnloading", strBasePath + "/MarkForUnloading", dAction, c =>
    {
      c.addOrUpdateArgument<bool>("flagValue", EArgumentType.Input, "");
      c.addOrUpdateArgument<TGenericEntity[]>("applicationData", EArgumentType.Input, "");
    });

    // Add tool node to parent
    Kernel.NodeSpace.addNodeToGroup(BaseDirectory.GlobalId, ToolGroup.GlobalId);

    return ToolGroup;
  }

  /// <summary>
  /// Creates a new directory
  /// </summary>
  private async Task createDirectoryAsync(IKernelAccess Kernel, IGroupRelation Device, ICommandCallContext CallContext, CancellationToken Token)
  {
    string strDirectoryName = CallContext.getInputArgumentValue<string>("directoryName");
    IGroupRelation? nBaseDirectory = CallContext.CallingNode.Relations.FirstOrDefault(n => n is IGroupRelation && n is not INodeSpace) as IGroupRelation;

    if (nBaseDirectory != null)
    {
      // Node Space
      IGroupRelation DirectoryGroup = createDirectoryInternal(Kernel, Device, strDirectoryName, nBaseDirectory);
      CallContext.setOutputArgumentValue("directoryNodeId", DirectoryGroup.GlobalId);

      // File System
      string strFullDirectoryName = Path.Combine(strBaseDirectoryPath, nBaseDirectory.Name, strDirectoryName);
      if (!createDirectoryOnDisk(strFullDirectoryName))
      {
        Logger.writeInfo($"Could not create directory '{strFullDirectoryName}' on file system.");
      }
      else
      {
        // evtl. rollbackDirectoryInternal(DirectoryGroup);
      }

      Logger.writeInfo($"New directory '{strDirectoryName}' created.");
    }
    else
    {
      Logger.writeError($"Creating directory '{strDirectoryName}' failed. Could not find the base directory.");
    }
    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>
  /// Creates a directory on disk.
  /// </summary>
  private bool createDirectoryOnDisk(string strFullDirectoryName)
  {
    bool bSuccess = false;
    try
    {
      if (!File.Exists(strFullDirectoryName))
      {
        Directory.CreateDirectory(strFullDirectoryName);
        bSuccess = true;
      }
      else
      {
        Logger.writeError($"Creating directory '{strFullDirectoryName}' failed. A file with the same name already exists.");
      }
    }
    catch (Exception Ex)
    {
      Logger.writeError($"Creating directory '{strFullDirectoryName}' failed. {Ex.Message}");
    }
    return bSuccess;
  }


  /// <summary>
  /// Creates a file on disk and optionally keeps it open.
  /// Caller is responsible for disposing the returned FileStream.
  /// </summary>
  private bool createFileOnDisk(string strFullFileName, bool bOpenRequest, out FileStream? oFs)
  {
    bool bSuccess = false;
    oFs = null;
    try
    {
      if (!File.Exists(strFullFileName))
      {
        oFs = File.Create(strFullFileName);

        if (!bOpenRequest)
        {
          oFs.Dispose(); // close after creation
          oFs = null;
        }
        bSuccess = true;
      }
      else
      {
        Logger.writeError($"Creating file '{strFullFileName}' failed. File already exists.");
      }

    }
    catch (Exception ex)
    {
      oFs?.Dispose();
      oFs = null;
      Logger.writeError($"Creating file '{strFullFileName}' failed. {ex.Message}");
      bSuccess = false;
    }
    return bSuccess;
  }

  /// <summary>
  /// Creates a new directory without call context
  /// </summary>
  private IGroupRelation createDirectoryInternal(IKernelAccess Kernel, IGroupRelation Device, string strDirectoryName, IGroupRelation BaseDirectory)
  {
    string strBasePath = BaseDirectory.getProperty<string>("opc-ua:flames:Mapping") + $"/{strDirectoryName}";

    IGroupRelation DirectoryGroup = createGroupWithMapping(Kernel, strDirectoryName, strBasePath, "ns=0;i=13353"); //FileDirectoryType -> alternative: "0,FileDirectoryType"

    DAsyncCommandCall dAction = createDefaultCommandAction(Kernel, Device);

    // CreateDirectory
    addCommand(Kernel, DirectoryGroup.GlobalId, "CreateDirectory", strBasePath + "/CreateDirectory", dAction, c =>
    {
      c.addOrUpdateArgument<string>("directoryName", EArgumentType.Input, "");
      c.addOrUpdateArgument<Guid>("directoryNodeId", EArgumentType.Output, "");
    });

    // Delete
    addCommand(Kernel, DirectoryGroup.GlobalId, "Delete", strBasePath + "/Delete", dAction, c =>
    {
      c.addOrUpdateArgument<Guid>("objectToDelete", EArgumentType.Input, "");
    });

    // CreateFile
    addCommand(Kernel, DirectoryGroup.GlobalId, "CreateFile", strBasePath + "/CreateFile", dAction, c =>
    {
      c.addOrUpdateArgument<string>("fileName", EArgumentType.Input, "");
      c.addOrUpdateArgument<bool>("requestFileOpen", EArgumentType.Input, "");
      c.addOrUpdateArgument<Guid>("fileNodeId", EArgumentType.Output, "");
      c.addOrUpdateArgument<uint>("fileHandle", EArgumentType.Output, "");
    });

    // MoveOrCopy
    addCommand(Kernel, DirectoryGroup.GlobalId, "MoveOrCopy", strBasePath + "/MoveOrCopy", dAction, c =>
    {
      c.addOrUpdateArgument<Guid>("objectToMoveOrCopy", EArgumentType.Input, "");
      c.addOrUpdateArgument<Guid>("targetDirectory", EArgumentType.Input, "");
      c.addOrUpdateArgument<bool>("createCopy", EArgumentType.Input, "");
      c.addOrUpdateArgument<string>("newName", EArgumentType.Input, "");
      c.addOrUpdateArgument<Guid>("newNodeId", EArgumentType.Output, "");
    });

    // Add directory node to parent
    Kernel.NodeSpace.addNodeToGroup(BaseDirectory.GlobalId, DirectoryGroup.GlobalId);
    return DirectoryGroup;
  }

  /// <summary>
  /// Deletes a directory or a file
  /// </summary>
  private async Task deleteAsync(IKernelAccess Kernel, IGroupRelation Device, ICommandCallContext CallContext, CancellationToken Token)
  {
    Guid ObjectToDelete = CallContext.getInputArgumentValue<Guid>("objectToDelete");
    IGroupRelation? nBaseDirectory = CallContext.CallingNode.Relations.FirstOrDefault(n => n is IGroupRelation && n is not INodeSpace) as IGroupRelation;
    deleteInternal(Kernel, ObjectToDelete, nBaseDirectory);
    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>
  /// Deletes a directory or a file without call context
  /// </summary>
  private void deleteInternal(IKernelAccess Kernel, Guid ObjectToDelete, IGroupRelation? nBaseDirectory)
  {
    if (nBaseDirectory != null && nBaseDirectory.tryGetNode<IGroupRelation>(ObjectToDelete, out IGroupRelation NodeToDelete))
    {
      Kernel.NodeSpace.removeNodeFromGroup(nBaseDirectory.GlobalId, NodeToDelete.GlobalId);
      Kernel.NodeSpace.removeNodeWithSubNodes(NodeToDelete.GlobalId);
      Logger.writeInfo($"Filesystem entry '{NodeToDelete.Name}' deleted.");
    }
    else
    {
      throw new ArgumentException($"Failed to delete the node '{ObjectToDelete}'.");
    }
  }

  /// <summary>
  /// Creates a file
  /// </summary>
  private async Task createFileAsync(IKernelAccess Kernel, IGroupRelation Device, ICommandCallContext CallContext, CancellationToken Token)
  {
    string strFileName = CallContext.getInputArgumentValue<string>("fileName");
    IGroupRelation? nBaseDirectory = CallContext.CallingNode.Relations.FirstOrDefault(n => n is IGroupRelation && n is not INodeSpace) as IGroupRelation;

    if (nBaseDirectory != null)
    {
      IGroupRelation FileGroup = createFileInternal(Kernel, Device, strFileName, nBaseDirectory);

      CallContext.setOutputArgumentValue("fileNodeId", FileGroup.GlobalId);
      CallContext.setOutputArgumentValue("fileHandle", FileGroup.GlobalId); // TODO

      // File System
      string strFullDirectoryName = Path.Combine(strBaseDirectoryPath, nBaseDirectory.Name, strFileName);
      if (!createFileOnDisk(strFullDirectoryName, false, out FileStream? Fs))
      {
        Logger.writeInfo($"Could not create file '{strFileName}' on file system.");
      }

      Logger.writeInfo($"New file '{strFileName}' created.");
    }
    else
    {
      Logger.writeError($"Creating file '{strFileName}' failed. Could not find the base directory.");
    }
    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>
  /// Creates a file without call context
  /// </summary>
  private IGroupRelation createFileInternal(IKernelAccess Kernel, IGroupRelation Device, string strFileName, IGroupRelation BaseDirectory)
  {
    string strBasePath = BaseDirectory.getProperty<string>("opc-ua:flames:Mapping") + $"/{strFileName}";

    IGroupRelation FileGroup = createGroupWithMapping(Kernel, strFileName, strBasePath, "ns=0;i=11575"); //FileType -> alternative: "0,FileType"

    // Size
    addDataNode(Kernel, FileGroup.GlobalId, "Size", typeof(ulong), strBasePath + "/Size");

    // Writable
    addDataNode(Kernel, FileGroup.GlobalId, "Writable", typeof(bool), strBasePath + "/Writable");

    // UserWritable
    addDataNode(Kernel, FileGroup.GlobalId, "UserWritable", typeof(bool), strBasePath + "/UserWritable");

    // OpenCount
    addDataNode(Kernel, FileGroup.GlobalId, "OpenCount", typeof(ushort), strBasePath + "/OpenCount");

    DAsyncCommandCall dAction = createDefaultCommandAction(Kernel, Device);

    addCommand(Kernel, FileGroup.GlobalId, "Open", strBasePath + "/Open", dAction, c =>
    {
      c.addOrUpdateArgument<byte>("mode", EArgumentType.Input, "");
      c.addOrUpdateArgument<uint>("fileHandle", EArgumentType.Output, "");
    });

    // Close
    addCommand(Kernel, FileGroup.GlobalId, "Close", strBasePath + "/Close", dAction, c =>
    {
      c.addOrUpdateArgument<byte>("fileHandle", EArgumentType.Input, "");
    });

    // Read
    addCommand(Kernel, FileGroup.GlobalId, "Read", strBasePath + "/Read", dAction, c =>
    {
      c.addOrUpdateArgument<uint>("fileHandle", EArgumentType.Input, "");
      c.addOrUpdateArgument<int>("length", EArgumentType.Input, "");
      c.addOrUpdateArgument<string>("data", EArgumentType.Output, "");
    });

    // Write
    addCommand(Kernel, FileGroup.GlobalId, "Write", strBasePath + "/Write", dAction, c =>
    {
      c.addOrUpdateArgument<uint>("fileHandle", EArgumentType.Input, "");
      c.addOrUpdateArgument<string>("data", EArgumentType.Input, "");
    });

    // GetPosition
    addCommand(Kernel, FileGroup.GlobalId, "GetPosition", strBasePath + "/GetPosition", dAction, c =>
    {
      c.addOrUpdateArgument<uint>("fileHandle", EArgumentType.Input, "");
      c.addOrUpdateArgument<long>("position", EArgumentType.Output, "");
    });

    // SetPosition
    addCommand(Kernel, FileGroup.GlobalId, "SetPosition", strBasePath + "/SetPosition", dAction, c =>
    {
      c.addOrUpdateArgument<uint>("fileHandle", EArgumentType.Input, "");
      c.addOrUpdateArgument<long>("position", EArgumentType.Input, "");
    });

    // Add file node to parent
    Kernel.NodeSpace.addNodeToGroup(BaseDirectory.GlobalId, FileGroup.GlobalId);
    return FileGroup;
  }

  /// <summary>
  /// Deletes a directory
  /// </summary>
  private async Task moveOrCopyAsync(IKernelAccess Kernel, IGroupRelation Device, ICommandCallContext CallContext, CancellationToken Token)
  {
    Guid ObjectToMove = CallContext.getInputArgumentValue<Guid>("objectToMoveOrCopy");
    Guid TargetDirectoryId = CallContext.getInputArgumentValue<Guid>("targetDirectory");
    bool bCreateCopy = CallContext.getInputArgumentValue<bool>("createCopy");
    string strNewName = CallContext.getInputArgumentValue<string>("newName");

    if (Kernel.NodeSpace.tryGetNode<IGroupRelation>(ObjectToMove, out IGroupRelation? nSourceNode) &&
        Kernel.NodeSpace.tryGetNode<IGroupRelation>(TargetDirectoryId, out IGroupRelation? nTargetDirectory))
    {

      string strTypeDef = nSourceNode.getProperty<string>("opc-ua:flames:TypeDefinition");
      string strFinalName = string.IsNullOrWhiteSpace(strNewName) ? nSourceNode.Name : strNewName;
      IGroupRelation? NewNodeId = null;

      // Directory
      if (strTypeDef == "ns=0;i=13353") // FileDirectoryType
      {
        NewNodeId = createDirectoryInternal(Kernel, Device, strFinalName, nTargetDirectory);

        if (!bCreateCopy)
        {
          IGroupRelation? nSourceParent = nSourceNode.Relations.FirstOrDefault(n => n is IGroupRelation && n is not INodeSpace) as IGroupRelation;

          deleteInternal(Kernel, nSourceNode.GlobalId, nSourceParent);
        }
      }
      // File
      else if (strTypeDef == "ns=0;i=11575") // FileType
      {
        NewNodeId = createFileInternal(Kernel, Device, strFinalName, nTargetDirectory);

        if (!bCreateCopy)
        {
          IGroupRelation? nSourceParent = nSourceNode.Relations.FirstOrDefault(n => n is IGroupRelation && n is not INodeSpace) as IGroupRelation;

          deleteInternal(Kernel, nSourceNode.GlobalId, nSourceParent);
        }
      }

      if (NewNodeId != null)
      {
        CallContext.setOutputArgumentValue("newNodeId", NewNodeId.GlobalId);
      }
    }
    else
    {
      throw new ArgumentException("MoveOrCopy failed. Source or target not found.");
    }

    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>
  /// Creates a default command action
  /// </summary>
  private DAsyncCommandCall createDefaultCommandAction(IKernelAccess Kernel, IGroupRelation Device)
  {
    DAsyncCommandCall dAction = async (n, t) =>
    {
      await this.executeCommandAsync(Kernel, Device, n, t).ConfigureAwait(false);
      return await Task.FromResult(new TCommandResult()).ConfigureAwait(false);
    };
    return dAction;
  }

  /// <summary>
  /// Creates a group with custom mapping and type
  /// </summary>
  private IGroupRelation createGroupWithMapping(IKernelAccess Kernel, string strName, string strMapping, string strTypeDefinition = "")
  {
    IGroupRelation Group = Kernel.NodeFactory.createOrGetGroup(Guid.NewGuid(), strName);
    Group.addProperty<string>("opc-ua:flames:Mapping", strMapping);
    Group.addProperty<string>("opc-ua:flames:TypeDefinition", strTypeDefinition);
    return Group;
  }

  /// <summary>
  /// Adds a command
  /// </summary>
  private void addCommand(IKernelAccess Kernel,
                          Guid ParentGroupId,
                          string strName,
                          string strMapping,
                          DAsyncCommandCall dAction,
                          Action<ICommandNode> ArgumentBuilder)
  {
    ICommandNode Command = Kernel.NodeFactory.createOrUpdateCommandNode(Guid.NewGuid(), strName, dAction);
    ArgumentBuilder(Command);
    Command.addProperty<string>("opc-ua:flames:Mapping", strMapping);
    Kernel.NodeSpace.addNodeToGroup(ParentGroupId, Command.GlobalId);
  }

  /// <summary>
  /// Adds a data node
  /// </summary>
  private void addDataNode(IKernelAccess Kernel,
                           Guid ParentGroupId,
                           string strName,
                           Type tType,
                           string strMapping = "",
                           string strTypeDefinition = "", 
                           string strDataTypeDefinition = "")
  {
    IDataNode DataNode = (IDataNode)Kernel.NodeFactory.createOrGetDataNode(Guid.NewGuid(), strName, tType, EDataClass.Event);

    DataNode.addProperty<string>("opc-ua:flames:Mapping", strMapping);
    DataNode.addProperty<string>("opc-ua:flames:DataTypeDefinition", strDataTypeDefinition);
    DataNode.addProperty<string>("opc-ua:flames:TypeDefinition", strTypeDefinition);
    
    Kernel.NodeSpace.addNodeToGroup(ParentGroupId, DataNode.GlobalId);

    DataNode.passState(EDataState.Good);
    //DataNode.passValue(new TSimpleVariant(tType.getDefault()), true, EDataState.Good);
  }
}
