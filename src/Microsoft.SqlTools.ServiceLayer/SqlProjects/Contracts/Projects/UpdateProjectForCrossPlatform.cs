//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Update a SQL project to be cross-platform compatible
    /// </summary>
    public class UpdateProjectForCrossPlatformRequest
    {
        public static readonly RequestType<SqlProjectParams, ResultStatus> Type = RequestType<SqlProjectParams, ResultStatus>.Create("sqlProjects/updateProjectForCrossPlatform");
    }
}
