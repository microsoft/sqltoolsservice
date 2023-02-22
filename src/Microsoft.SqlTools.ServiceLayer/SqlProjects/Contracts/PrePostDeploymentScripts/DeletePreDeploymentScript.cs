//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Delete a pre-deployment script from a project
    /// </summary>
    public class DeletePreDeploymentScriptRequest
    {
        public static readonly RequestType<SqlProjectScriptParams, ResultStatus> Type = RequestType<SqlProjectScriptParams, ResultStatus>.Create("sqlProjects/deletePreDeploymentScript");
    }
}
