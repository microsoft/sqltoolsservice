//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Get all the SQLCMD variables in a project
    /// </summary>
    public class GetSqlCmdVariablesRequest
    {
        public static readonly RequestType<SqlProjectParams, GetSqlCmdVariablesResult> Type = RequestType<SqlProjectParams, GetSqlCmdVariablesResult>.Create("sqlProjects/getSqlCmdVariables");
    }

    /// <summary>
    /// Result containing SQLCMD variables in the project
    /// </summary>
    public class GetSqlCmdVariablesResult : ResultStatus
    {
        /// <summary>
        /// Array of SQLCMD variables contained in the project
        /// </summary>
        public SqlCmdVariable[] SqlCmdVariables { get; set; }
    }
}
