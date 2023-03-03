//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Get all the SQL object scripts in a project
    /// </summary>
    public class GetSqlObjectScriptsRequest
    {
        public static readonly RequestType<SqlProjectParams, GetScriptsResult> Type = RequestType<SqlProjectParams, GetScriptsResult>.Create("sqlProjects/getSqlObjectScripts");
    }

    /// <summary>
    /// Result containing scripts in the project
    /// </summary>
    public class GetScriptsResult : ResultStatus
    {
        /// <summary>
        /// Array of scripts contained in the project
        /// </summary>
        public string[] Scripts { get; set; }
    }
}
