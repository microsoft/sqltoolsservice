//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class ExcludeSqlObjectScriptRequest
    {
        public static readonly RequestType<SqlProjectScriptParams, SqlProjectResult> Type = RequestType<SqlProjectScriptParams, SqlProjectResult>.Create("sqlprojects/excludeSqlObjectScript");
    }
}
