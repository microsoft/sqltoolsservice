//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Add a pre-deployment script to a project
    /// </summary>
    public class AddPreDeploymentScriptRequest
    {
        public static readonly RequestType<SqlProjectScriptParams, ResultStatus> Type = RequestType<SqlProjectScriptParams, ResultStatus>.Create("sqlProjects/addPreDeploymentScript");
    }
}
