//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class AddSqlCmdVariableParams : SqlProjectParams
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public string DefaultVault { get; set; }
    }

    public class AddSqlCmdVariableRequest
    {
        public static readonly RequestType<AddSqlCmdVariableParams, ResultStatus> Type = RequestType<AddSqlCmdVariableParams, ResultStatus>.Create("sqlprojects/addSqlCmdVariable");
    }
}
