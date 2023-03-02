//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Get all the None items in a project
    /// </summary>
    public class GetNoneItemsRequest
    {
        public static readonly RequestType<SqlProjectParams, GetScriptsResult> Type = RequestType<SqlProjectParams, GetScriptsResult>.Create("sqlProjects/getNoneItems");
    }
}
