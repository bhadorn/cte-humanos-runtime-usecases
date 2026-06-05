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

using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.DataModel;
using HumanOS.Kernel.Utils;
using HumanOS.Kernel.UHAL.Script;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example for Http payload parsing
  /// </summary>
  public class Blank_UHALHttpScriptObject : TAbstractHttpScriptObject
  {
    ///<see cref="TAbstractHttpScriptObject"/>
    public override string composeRequest(ref string rstrAddress, 
                                          Dictionary<string, string> dicProperties, 
                                          Dictionary<string, object> dicInputArguments,
                                          Dictionary<string, IEnumerable<string>> dicRequestHeaders)
    {
      dicRequestHeaders["Cookie"] = new List<string>()
      {
        TCommandHelper.getArgument<string>(dicInputArguments, "AuthCookie")
      };

      int iTemplateId = TCommandHelper.getArgumentOrDefault<int>(dicInputArguments, "TemplateId", 0);
      int iVaultId = TCommandHelper.getArgumentOrDefault<int>(dicInputArguments, "VaultId", -1);

      JObject jRoot = new JObject();
      jRoot.Add(new JProperty("name", dicInputArguments["Name"]));
      jRoot.Add(new JProperty("project_id", dicInputArguments["ProjectId"]));
      jRoot.Add(new JProperty("inventory_id", dicInputArguments["InventoryId"]));
      jRoot.Add(new JProperty("repository_id", dicInputArguments["RepositoryId"]));
      jRoot.Add(new JProperty("environment_id", dicInputArguments["EnvironmentId"]));
      jRoot.Add(new JProperty("description", dicInputArguments["Description"]));
      jRoot.Add(new JProperty("playbook", dicInputArguments["PlayBookFileName"]));
      jRoot.Add(new JProperty("app", "ansible"));
      
      if (iTemplateId > 0)
      {
        jRoot.Add(new JProperty("id", iTemplateId));
      }

      if (iVaultId >= 0)
      {
        JObject jAnsibleVault = new JObject();
        jAnsibleVault.Add(new JProperty("vault_key_id", iVaultId));
        jAnsibleVault.Add(new JProperty("type", "password"));
        jAnsibleVault.Add(new JProperty("name", dicInputArguments["VaultName"]));

        JArray jVaults = new JArray();
        jVaults.Add(jAnsibleVault);
        jRoot.Add("vaults", jVaults);
      } //iVaultId >= 0

      Logger.writeInfo($"Payload dump:\n{jRoot}");

      return jRoot.ToString();
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override void parseResponse(string strAddress, Dictionary<string, string> dicProperties, string strContent, EContentType eContentType, Dictionary<string, object> dicInputArguments, Dictionary<string, object> dicOutputArguments)
    {
      Logger.writeInfo(strContent);
    }

    ///<see cref="TAbstractHttpScriptObject"/>
    public override TCommandResult parseError(Dictionary<string, string> dicProperties, Dictionary<string, object> dicInputArguments, string strMethod, string strAddress, string strRequestBody, THttpResponse Response)
    {
      Logger.writeError(Response.Content);
      return base.parseError(dicProperties, dicInputArguments, strMethod, strAddress, strRequestBody, Response);
    }
  }
}
