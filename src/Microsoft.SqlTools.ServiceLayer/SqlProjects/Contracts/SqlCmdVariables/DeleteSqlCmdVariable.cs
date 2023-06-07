//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for deleting a SQLCMD variable from a project
    /// </summary>
    public class DeleteSqlCmdVariableParams : SqlProjectParams
    {
        /// <summary>
        /// Name of the SQLCMD variable to be deleted
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Delete a SQLCMD variable from a project
    /// </summary>
    public class DeleteSqlCmdVariableRequest
    {
        public static readonly RequestType<DeleteSqlCmdVariableParams, ResultStatus> Type = RequestType<DeleteSqlCmdVariableParams, ResultStatus>.Create("sqlProjects/deleteSqlCmdVariable");
    }
}
