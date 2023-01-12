//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class CloseSqlProjectRequest
    {
        public static readonly RequestType<SqlProjectParams, SqlProjectResult> Type = RequestType<SqlProjectParams, SqlProjectResult>.Create("sqlprojects/closeProject");
    }
}
