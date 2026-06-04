/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2024 – www.cybertech.swiss         *
 *****************************************************************************
 * Project: HumanOS (R)
 * Date   : 2024
 *****************************************************************************
 * License:                                                                  *
 *   This library is protected software; you are not allowed to redistribute *
 *   whole or part of it to other companies or external persons without the  *
 *   authorization of the CEO CyberTech Engineering GmbH.                    *
 *****************************************************************************/

using HumanOS.Kernel;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Processing;
using HumanOS.Kernel.UHAL.Script;
using HumanOS.Kernel.Utils;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example for UHAL logic
  /// </summary>
  public class TUhalPrepareJobScript : TAbstractLogicScriptObject
  {
    #region Constants
    
    private const string ARG_AuthCookie = "AuthCookie";
    private const string ARG_Name = "Name";
    private const string ARG_Path = "Path";
    private const string ARG_Content = "Content";
    private const string ARG_Names = "Names";
    private const string ARG_VaultId = "VaultId";
    private const string ARG_VaultName = "VaultName";
    private const string ARG_UserName = "UserName";
    private const string ARG_Password = "Password";
    private const string ARG_ProjectId = "ProjectId";
    private const string ARG_EnvironmentId = "EnvironmentId";
    private const string ARG_RepositoryId = "RepositoryId";
    private const string ARG_InventoryId = "InventoryId";
    private const string ARG_TemplateId = "TemplateId";
    private const string ARG_KeyId = "KeyId";
    private const string ARG_TaskId = "TaskId";
    private const string ARG_Description = "Description";
    private const string ARG_PlayBookFileName = "PlayBookFileName";
    
    private const string ARG_EnvironmentIds = "EnvironmentIds";
    private const string ARG_ProjectIds = "ProjectIds";
    private const string ARG_RepositoryIds = "RepositoryIds";
    
    #endregion
  
    ///<see cref="TAbstractLogicScriptObject"/>
    public override void executeCommand(IKernelAccess Kernel, 
                                        IGroupRelation DeviceNode, 
                                        Dictionary<string, string> dicProperties, 
                                        Dictionary<string, object> dicInputArguments, 
                                        Dictionary<string, object> dicOutputArguments)
    {
      string strAuthenticationKey = "";
      try
      {
        Guid EntityId = TCommandHelper.getArgument<Guid>(dicInputArguments, "EntityId");
        string JobName = TCommandHelper.getArgument<string>(dicInputArguments, "JobName");
        string strEnvironment = TCommandHelper.getArgument<string>(dicInputArguments, "Environment");
        string strInventory = TCommandHelper.getArgument<string>(dicInputArguments, "Inventory");
        string strArguments = TCommandHelper.getArgument<string>(dicInputArguments, "Arguments");
        
        JObject jArgRoot;
        try
        {
          jArgRoot = JObject.Parse(strArguments);
        }
        catch (Exception Exc)
        {
          if (strArguments.isEmpty())
          {
            jArgRoot = new JObject();
          }
          else
          {
            throw new ArgumentException($"Could not parse arguments. {Exc.Message}");
          }
        }
        
        //1. login
        strAuthenticationKey = login(DeviceNode);
        
        //2. create the project
        string strProjectName = $"Project_{EntityId}";
        int iProjectId = createOrGetProject(DeviceNode, strAuthenticationKey, strProjectName);
        Logger.writeDebug($"Project '{iProjectId}' is ready.");
        int iKeyId = getKey(DeviceNode, strAuthenticationKey, iProjectId, "None");

        //3. Create or Update Repository
        string strRepositoryName = DeviceNode.getProperty<string>("Semaphore.RepositoryName");
        string strRepositoryPath = DeviceNode.getProperty<string>("Semaphore.RepositoryPath");
        int iRepositoryId = createOrUpdateRespository(DeviceNode, strAuthenticationKey, iProjectId, iKeyId, strRepositoryName, strRepositoryPath);
        Logger.writeDebug($"Repository '{iRepositoryId}' is ready.");
        
        //4. Create or Update Ansible Vault
        int iVaultId = createOrUpdateKey(DeviceNode, strAuthenticationKey, iProjectId, "AnsibleVaultPassword", (string)jArgRoot["VaultUser"], (string)jArgRoot["VaultKey"]);
        Logger.writeDebug($"Ansible Vault '{iVaultId}' is ready.");
        
        //5. Create Or Update Environment
        string strEnvironmentName = "StandardEnv";
        int iEnvironmentId = createOrUpdateEnvironment(DeviceNode, strAuthenticationKey, iProjectId, strEnvironmentName, strEnvironment);
        Logger.writeDebug($"Environment '{iEnvironmentId}' is ready.");
        
        //6. Create or Update Inventory
        string strInventoryName = "StandardInventory";
        int iInventoryId = createOrUpdateInventory(DeviceNode, strAuthenticationKey, iProjectId, iKeyId, strInventoryName, strInventory);
        Logger.writeDebug($"Inventory '{iEnvironmentId}' is ready.");
        
        //7. Create of Update TaskTemplate
        string strTemplateName = $"Template_{JobName}";
        int iTemplateId = createOrUpdateTemplate(DeviceNode, 
                                                 strAuthenticationKey, 
                                                 iProjectId, 
                                                 iRepositoryId, 
                                                 iEnvironmentId, 
                                                 iInventoryId, 
                                                 iVaultId,
                                                 strTemplateName,
                                                 jArgRoot);
        Logger.writeDebug($"TaskTemplate '{iTemplateId}' is ready.");
        
        //7. Starts the job
        int iTaskId = startTask(DeviceNode, 
                                strAuthenticationKey, 
                                iProjectId, 
                                iTemplateId,
                                jArgRoot);
        
        dicOutputArguments["TaskId"] = iTaskId;
        dicOutputArguments["ProjectId"] = iProjectId;
      }
      finally
      {
        tryLogout(DeviceNode, strAuthenticationKey);
      }
    }
    
    ///<summary>
    /// creates or updates the inventory
    ///</summary>
    private int createOrUpdateInventory(IGroupRelation DeviceNode, string strAuthenticationKey, int iProjectId, int iKeyId, string strInventoryName, string strInventory)
    {
      //Gets the project by name
      TCommandArgs Args = new TCommandArgs();
      Args.Input[ARG_AuthCookie] = strAuthenticationKey;
      Args.Input[ARG_Name] = strInventoryName;
      Args.Input[ARG_ProjectId] = iProjectId;
      TCommandResult Result = TCommandHelper.call(DeviceNode, "GetInventoryByName", Args);
      if (Result.State != EProcessingState.Good)
      {
        throw new ArgumentException($"Failed to get project. {Result.ErrorInfo}");
      }
      
      int iRetval = TCommandHelper.getArgument<int>(Args.Output, ARG_InventoryId);

      if (iRetval == 0)
      {
        TCommandArgs Args2 = new TCommandArgs();
        Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args2.Input[ARG_Name] = strInventoryName;
        Args2.Input[ARG_Content] = strInventory;
        Args2.Input[ARG_ProjectId] = iProjectId;
        Args2.Input[ARG_KeyId] = iKeyId;

        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "CreateInventory", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to create inventory '{strInventoryName}'. {Result2.ErrorInfo}");
        }
        iRetval = TCommandHelper.getArgument<int>(Args2.Output, ARG_InventoryId);
      } //iRetval == 0
      else
      {
        TCommandArgs Args2 = new TCommandArgs();
        Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args2.Input[ARG_Name] = strInventoryName;
        Args2.Input[ARG_Content] = strInventory;
        Args2.Input[ARG_ProjectId] = iProjectId;
        Args2.Input[ARG_InventoryId] = iRetval;
        Args2.Input[ARG_KeyId] = iKeyId;

        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "UpdateInventory", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to update inventory '{strInventoryName}'. {Result2.ErrorInfo}");
        }
      } //iRetval != 0
      return iRetval;
    }
    
    ///<summary>
    /// creates or gets the project id
    ///</summary>
    private int createOrGetProject(IGroupRelation DeviceNode, string strAuthenticationKey, string strProjectName)
    {
      //Gets the project by name
      TCommandArgs Args = new TCommandArgs();
      Args.Input[ARG_AuthCookie] = strAuthenticationKey;
      Args.Input[ARG_Name] = strProjectName;
      TCommandResult Result = TCommandHelper.call(DeviceNode, "GetProjectByName", Args);
      if (Result.State != EProcessingState.Good)
      {
        throw new ArgumentException($"Failed to get project. {Result.ErrorInfo}");
      }
      
      int iRetval = TCommandHelper.getArgument<int>(Args.Output, ARG_ProjectId);

      if (iRetval == 0)
      {
        TCommandArgs Args2 = new TCommandArgs();
        Args2.Input[ARG_Name] = strProjectName;
        Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "CreateProject", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to create project '{strProjectName}'. {Result2.ErrorInfo}");
        }
        
        //Gets the project
        Args = new TCommandArgs();
        Args.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args.Input[ARG_Name] = strProjectName;
        Result = TCommandHelper.call(DeviceNode, "GetProjectByName", Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to get projects. {Result.ErrorInfo}");
        }
        iRetval = TCommandHelper.getArgument<int>(Args.Output, ARG_ProjectId);
      } //iRetval == 0
      
      if (iRetval == 0)
      {
        throw new ArgumentException($"Failed to create and get project '{strProjectName}'. {Result.ErrorInfo}");
      }
      
      return iRetval;
    }
    
    ///<summary>
    /// creates or updates the environment
    ///</summary>
    private int createOrUpdateEnvironment(IGroupRelation DeviceNode, 
                                          string strAuthenticationKey, 
                                          int iProjectId, 
                                          string strEnvironmentName,
                                          string strEnvironment)
    {
      JObject jEnvironment;
      try
      {
        jEnvironment = JObject.Parse(strEnvironment);
      }
      catch(Exception)
      {
        jEnvironment = new JObject();
      }
      //Default environment to get the output backk as JSON
      jEnvironment["ANSIBLE_CALLBACK_WHITELIST"] = "json";
      jEnvironment["ANSIBLE_HOST_PATTERN_MISMATCH"] = "ignore";
      jEnvironment["ANSIBLE_STDOUT_CALLBACK"] = "json";
    
      //Gets the environments
      TCommandArgs Args = new TCommandArgs();
      Args.Input[ARG_AuthCookie] = strAuthenticationKey;
      Args.Input[ARG_ProjectId] = iProjectId;
      Args.Input[ARG_Name] = strEnvironmentName;
      TCommandResult Result = TCommandHelper.call(DeviceNode, "GetEnvironmentByName", Args);
      if (Result.State != EProcessingState.Good)
      {
        throw new ArgumentException($"Failed to get environments of project '{iProjectId}'. {Result.ErrorInfo}");
      }

      int iRetval = TCommandHelper.getArgument<int>(Args.Output, ARG_EnvironmentId);

      if (iRetval == 0)
      {
        TCommandArgs Args2 = new TCommandArgs();
        Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args2.Input[ARG_Name] = strEnvironmentName;
        Args2.Input[ARG_Content] = jEnvironment.ToString();
        Args2.Input[ARG_ProjectId] = iProjectId;
        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "CreateEnvironment", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to create environment '{strEnvironmentName}'. {Result2.ErrorInfo}");
        }
        
        //Gets the environment
        Args = new TCommandArgs();
        Args.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args.Input[ARG_ProjectId] = iProjectId;
        Args.Input[ARG_Name] = strEnvironmentName;
        Result = TCommandHelper.call(DeviceNode, "GetEnvironmentByName", Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to get environment. {Result.ErrorInfo}");
        }
        iRetval = TCommandHelper.getArgument<int>(Args.Output, ARG_EnvironmentId);
        if (iRetval == 0)
        {
          throw new ArgumentException($"Failed to create and get environment '{strEnvironmentName}'. {Result.ErrorInfo}");
        }
      } //iRetval == 0
      else
      {
        TCommandArgs Args2 = new TCommandArgs();
        Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args2.Input[ARG_Name] = strEnvironmentName;
        Args2.Input[ARG_Content] = jEnvironment.ToString();
        Args2.Input[ARG_ProjectId] = iProjectId;
        Args2.Input[ARG_EnvironmentId] = iRetval;
        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "UpdateEnvironment", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to update environment '{strEnvironmentName}'. {Result2.ErrorInfo}");
        }
      } //iRetval != 0
      
      return iRetval;
    }
    
    ///<summary>
    /// creates or updates the key for ansible vaults
    ///</summary>
    private int createOrUpdateKey(IGroupRelation DeviceNode, 
                                  string strAuthenticationKey, 
                                  int iProjectId,
                                  string strKeyName,
                                  string strUserName,
                                  string strPassword)
    {
      //Gets the Key
      int iRetval = 0;
      try
      {
        iRetval = getKey(DeviceNode, strAuthenticationKey, iProjectId, strKeyName);
      }
      catch(ArgumentException Exc)
      {
      }

      TCommandArgs Args2 = new TCommandArgs();
      Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
      Args2.Input[ARG_ProjectId] = iProjectId;
      Args2.Input[ARG_Name] = strKeyName;
      Args2.Input[ARG_UserName] = strUserName;
      Args2.Input[ARG_Password] = strPassword;
      if (iRetval == 0)
      {
        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "CreateKey", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to create a key {strKeyName} for project '{iProjectId}'. {Result2.ErrorInfo}");
        }
        iRetval = getKey(DeviceNode, strAuthenticationKey, iProjectId, strKeyName);
        if (iRetval == 0)
        {
          throw new ArgumentException($"Failed to create and get key '{strKeyName}'. {Result2.ErrorInfo}");
        }
      }
      else
      {
        Args2.Input[ARG_KeyId] = iRetval;
        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "UpdateKey", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to update a key {strKeyName} for project '{iProjectId}'. {Result2.ErrorInfo}");
        }
      }
      return iRetval;
    }
    
    ///<summary>
    /// creates or updates the template
    ///</summary>
    private int createOrUpdateTemplate(IGroupRelation DeviceNode, 
                                       string strAuthenticationKey, 
                                       int iProjectId, 
                                       int iRepositoryId,
                                       int iEnvironmentId, 
                                       int iInventoryId, 
                                       int iVaultId,
                                       string strTemplateName,
                                       JObject jArguments)
    {
      //Gets the environments
      TCommandArgs Args = new TCommandArgs();
      Args.Input[ARG_AuthCookie] = strAuthenticationKey;
      Args.Input[ARG_ProjectId] = iProjectId;
      Args.Input[ARG_Name] = strTemplateName;
      TCommandResult Result = TCommandHelper.call(DeviceNode, "GetTemplateByName", Args);
      if (Result.State != EProcessingState.Good)
      {
        throw new ArgumentException($"Failed to get template of project '{iProjectId}'. {Result.ErrorInfo}");
      }

      int iRetval = TCommandHelper.getArgument<int>(Args.Output, ARG_TemplateId);

      if (iRetval == 0)
      {
        TCommandArgs Args2 = new TCommandArgs();
        Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args2.Input[ARG_ProjectId] = iProjectId;
        Args2.Input[ARG_RepositoryId] = iRepositoryId;
        Args2.Input[ARG_EnvironmentId] = iEnvironmentId;
        Args2.Input[ARG_InventoryId] = iInventoryId;
        Args2.Input[ARG_VaultId] = iVaultId;
        Args2.Input[ARG_VaultName] = "default"; //currently uses default
        Args2.Input[ARG_Name] = strTemplateName;
        Args2.Input[ARG_Description] = $"TaskTemplate for {strTemplateName}";
        Args2.Input[ARG_PlayBookFileName] = (string)jArguments["PlayBook"];
        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "CreateTemplate", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to create a task template of project '{iProjectId}'. {Result2.ErrorInfo}");
        }
        
        Args = new TCommandArgs();
        Args.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args.Input[ARG_ProjectId] = iProjectId;
        Args.Input[ARG_Name] = strTemplateName;
        Result = TCommandHelper.call(DeviceNode, "GetTemplateByName", Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to get template of project '{iProjectId}'. {Result.ErrorInfo}");
        }
        iRetval = TCommandHelper.getArgument<int>(Args.Output, ARG_TemplateId);
        if (iRetval == 0)
        {
          throw new ArgumentException($"Failed to create and get template '{strTemplateName}'. {Result.ErrorInfo}");
        }
      } //iRetval == 0
      else
      {
        //BUG: First call must be without vaults
        //     Second call must contain the vault
        TCommandArgs Args2 = new TCommandArgs();
        Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args2.Input[ARG_ProjectId] = iProjectId;
        Args2.Input[ARG_RepositoryId] = iRepositoryId;
        Args2.Input[ARG_EnvironmentId] = iEnvironmentId;
        Args2.Input[ARG_InventoryId] = iInventoryId;
        Args2.Input[ARG_TemplateId] = iRetval;
        Args2.Input[ARG_VaultName] = "";
        Args2.Input[ARG_VaultId] = -1; 
        Args2.Input[ARG_Name] = strTemplateName;
        Args2.Input[ARG_Description] = $"TaskTemplate for {strTemplateName}";
        Args2.Input[ARG_PlayBookFileName] = (string)jArguments["PlayBook"];
        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "UpdateTemplate", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to create a task template of project '{iProjectId}'. {Result2.ErrorInfo}");
        }

        Args2.Input[ARG_VaultName] = "default"; //currently uses default
        Args2.Input[ARG_VaultId] = iVaultId;
        Result2 = TCommandHelper.call(DeviceNode, "UpdateTemplate", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to create a task template of project '{iProjectId}'. {Result2.ErrorInfo}");
        }
      }
      
      return iRetval;
    }
    
    ///<summary>
    /// creates or updates the repository
    ///</summary>
    private int createOrUpdateRespository(IGroupRelation DeviceNode, 
                                          string strAuthenticationKey, 
                                          int iProjectId, 
                                          int iKeyId,
                                          string strRepositoryName, 
                                          string strRepositoryPath)
    {
      
      //Gets the environments
      TCommandArgs Args = new TCommandArgs();
      Args.Input[ARG_AuthCookie] = strAuthenticationKey;
      Args.Input[ARG_ProjectId] = iProjectId;
      Args.Input[ARG_Name] = strRepositoryName;
      TCommandResult Result = TCommandHelper.call(DeviceNode, "GetRepositoryByName", Args);
      if (Result.State != EProcessingState.Good)
      {
        throw new ArgumentException($"Failed to get repository of project '{iProjectId}'. {Result.ErrorInfo}");
      }
      int iRetval = TCommandHelper.getArgument<int>(Args.Output, ARG_RepositoryId);

      if (iRetval == 0)
      {
        TCommandArgs Args2 = new TCommandArgs();
        Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args2.Input[ARG_Name] = strRepositoryName;
        Args2.Input[ARG_Path] = strRepositoryPath;
        Args2.Input[ARG_ProjectId] = iProjectId;
        Args2.Input[ARG_KeyId] = iKeyId;
        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "CreateRepository", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to create repository '{strRepositoryName}'. {Result2.ErrorInfo}");
        }
        
        //Gets the repositories
        Args = new TCommandArgs();
        Args.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args.Input[ARG_ProjectId] = iProjectId;
        Args.Input[ARG_Name] = strRepositoryName;
        Result = TCommandHelper.call(DeviceNode, "GetRepositoryByName", Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to get repositories. {Result.ErrorInfo}");
        }
        iRetval = TCommandHelper.getArgument<int>(Args.Output, ARG_RepositoryId);
        if (iRetval == 0)
        {
          throw new ArgumentException($"Failed to create and get repository '{strRepositoryName}'. {Result.ErrorInfo}");
        }
      } //iRetval == 0
      else
      {
        TCommandArgs Args2 = new TCommandArgs();
        Args2.Input[ARG_AuthCookie] = strAuthenticationKey;
        Args2.Input[ARG_Name] = strRepositoryName;
        Args2.Input[ARG_Path] = strRepositoryPath;
        Args2.Input[ARG_ProjectId] = iProjectId;
        Args2.Input[ARG_RepositoryId] = iRetval;
        Args2.Input[ARG_KeyId] = iKeyId;
        TCommandResult Result2 = TCommandHelper.call(DeviceNode, "UpdateRepository", Args2);
        if (Result2.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to update repository '{strRepositoryName}'. {Result2.ErrorInfo}");
        }
      } //iRetval != 0
      
      return iRetval;
    }
    
    ///<summary>
    /// gets the default key of the project
    ///</summary>
    private int getKey(IGroupRelation DeviceNode, string strAuthenticationKey, int iProjectId, string strKeyName)
    {
      TCommandArgs Args = new TCommandArgs();
      Args.Input[ARG_ProjectId] = iProjectId;
      Args.Input[ARG_AuthCookie] = strAuthenticationKey;
      Args.Input[ARG_Name] = strKeyName;
      TCommandResult Result = TCommandHelper.call(DeviceNode, "GetKeyByName", Args);
      if (Result.State != EProcessingState.Good)
      {
        throw new ArgumentException($"Failed to get key '{strKeyName}'. {Result.ErrorInfo}");
      }
      return Args.getOutputArgument<int>(ARG_KeyId);
    }
    
    ///<summary>
    /// Login to semaphore
    ///</summary>
    private string login(IGroupRelation DeviceNode)
    {
      TCommandArgs Args = new TCommandArgs();
      TCommandResult Result = TCommandHelper.call(DeviceNode, "LoginUser", Args);
      if (Result.State != EProcessingState.Good)
      {
        throw new ArgumentException($"Failed to login. {Result.ErrorInfo}");
      }
      Logger.writeDebug("Successfully login.");
      return Args.getOutputArgument<string>(ARG_AuthCookie);
    }
    
    ///<summary>
    /// starts the task
    ///</summary>
    private int startTask(IGroupRelation DeviceNode, 
                          string strAuthenticationKey, 
                          int iProjectId, 
                          int iTemplateId,
                          JObject jArguments)
    {
      TCommandArgs Args = new TCommandArgs();
      Args.Input[ARG_ProjectId] = iProjectId;
      Args.Input[ARG_AuthCookie] = strAuthenticationKey;
      Args.Input[ARG_TemplateId] = iTemplateId;
      Args.Input["Debug"] = (bool)jArguments["Debug"];
      Args.Input["DryRun"] = (bool)jArguments["DryRun"];
      Args.Input["Diff"] = (bool)jArguments["Diff"];
      TCommandResult Result = TCommandHelper.call(DeviceNode, "StartTask", Args);
      if (Result.State != EProcessingState.Good)
      {
        throw new ArgumentException($"Failed to start task. {Result.ErrorInfo}");
      }
      return Args.getOutputArgument<int>(ARG_TaskId);
    }
    
    ///<summary>
    /// Logout from semaphore
    ///</summary>
    private void tryLogout(IGroupRelation DeviceNode, string strAuthenticationKey)
    {
      try
      {
        TCommandArgs Args = new TCommandArgs();
        Args.Input[ARG_AuthCookie] = strAuthenticationKey;
        TCommandResult Result = TCommandHelper.call(DeviceNode, "LogoutUser", Args);
        if (Result.State != EProcessingState.Good)
        {
          throw new ArgumentException($"Failed to logout. {Result.ErrorInfo}");
        }
        Logger.writeDebug("Successfully logout.");
      }
      catch(Exception Exc) when (!Exc.isCancelException())
      {
        Logger.writeWarning(Exc.Message);
      }
    }
  }
}
