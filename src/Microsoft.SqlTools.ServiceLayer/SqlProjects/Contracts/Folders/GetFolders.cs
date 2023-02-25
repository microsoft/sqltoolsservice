//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class GetFoldersRequest
    {
        public static readonly RequestType<SqlProjectParams, GetFoldersResult> Type = RequestType<SqlProjectParams, GetFoldersResult>.Create("sqlProjects/getFolders");
    }

    /// <summary>
    /// Result containing folders in the project
    /// </summary>
    public class GetFoldersResult : ResultStatus
    {
        /// <summary>
        /// Array of folders contained in the project
        /// </summary>
        public string[] Folders { get; set; }
    }
}
