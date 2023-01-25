//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class DeleteSqlCmdVariableParams : SqlProjectParams
    {
        public string Name { get; set; }
    }

    public class DeleteSqlCmdVariableRequest
    {
        public static readonly RequestType<DeleteSqlCmdVariableParams, ResultStatus> Type = RequestType<DeleteSqlCmdVariableParams, ResultStatus>.Create("sqlprojects/deleteSqlCmdVariable");
    }
}
