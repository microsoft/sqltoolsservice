//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class UpdateSqlCmdVariableRequest
    {
        public static readonly RequestType<AddSqlCmdVariableParams, ResultStatus> Type = RequestType<AddSqlCmdVariableParams, ResultStatus>.Create("sqlprojects/updateSqlCmdVariable");
    }
}
