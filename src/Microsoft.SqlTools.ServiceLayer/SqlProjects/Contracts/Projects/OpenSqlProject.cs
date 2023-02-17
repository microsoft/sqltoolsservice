//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class OpenSqlProjectRequest
    {
        public static readonly RequestType<SqlProjectParams, ResultStatus> Type = RequestType<SqlProjectParams, ResultStatus>.Create("sqlProjects/openProject");
    }
}
