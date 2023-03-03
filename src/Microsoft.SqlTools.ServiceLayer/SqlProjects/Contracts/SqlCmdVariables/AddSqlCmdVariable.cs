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
    /// Parameters for adding a SQLCMD variable to a project
    /// </summary>
    public class AddSqlCmdVariableParams : SqlProjectParams
    {
        /// <summary>
        /// Name of the SQLCMD variable
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Default value of the SQLCMD variable
        /// </summary>
        public string DefaultValue { get; set; }
    }

    /// <summary>
    /// Add a SQLCMD variable to a project
    /// </summary>
    public class AddSqlCmdVariableRequest
    {
        public static readonly RequestType<AddSqlCmdVariableParams, ResultStatus> Type = RequestType<AddSqlCmdVariableParams, ResultStatus>.Create("sqlProjects/addSqlCmdVariable");
    }
}
