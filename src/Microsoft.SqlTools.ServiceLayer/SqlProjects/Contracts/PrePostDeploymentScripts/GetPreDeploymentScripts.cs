﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Get all the pre-deployment scripts in a project
    /// </summary>
    public class GetPreDeploymentScriptsRequest
    {
        public static readonly RequestType<SqlProjectParams, GetScriptsResult> Type = RequestType<SqlProjectParams, GetScriptsResult>.Create("sqlProjects/getPreDeploymentScripts");
    }
}
