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
using HumanOS.Kernel.Communication.Http;
using HumanOS.Kernel.PeSeL.Script;
using HumanOS.PeSeL.WebService.Script;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Example of HumanOS REST API script
  /// </summary>
  public class TBlankPeSeLWebScriptObject : TAbstractWebScriptObject
  {
    ///<see cref="TAbstractWebScriptObject"/>
    public override string handleDelete(IKernelAccess Kernel,
                                        HttpContext HttpContext,
                                        TPayloadProcessingContext Context,
                                        List<string> lstPath,
                                        Dictionary<string, string> dicParams,
                                        ref EContentType reContentType,
                                        string strData)
    {
      throw new NotImplementedException();
    }

    ///<see cref="TAbstractWebScriptObject"/>
    public override string handleGet(IKernelAccess Kernel,
                                     HttpContext HttpContext,
                                     TPayloadProcessingContext Context,
                                     List<string> lstPath,
                                     Dictionary<string, string> dicParams,
                                     ref EContentType reContentType)
    {
      throw new NotImplementedException();
    }

    ///<see cref="TAbstractWebScriptObject"/>
    public override string handlePatch(IKernelAccess Kernel,
                                       HttpContext HttpContext,
                                       TPayloadProcessingContext Context,
                                       List<string> lstPath,
                                       Dictionary<string, string> dicParams,
                                       ref EContentType reContentType,
                                       string strData)
    {
      throw new NotImplementedException();
    }

    ///<see cref="TAbstractWebScriptObject"/>
    public override string handlePost(IKernelAccess Kernel,
                                      HttpContext HttpContext,
                                      TPayloadProcessingContext Context,
                                      List<string> lstPath,
                                      Dictionary<string, string> dicParams,
                                      ref EContentType reContentType,
                                      string strData,
                                      HttpResponse Response)
    {
      throw new NotImplementedException();
    }
  }
}
