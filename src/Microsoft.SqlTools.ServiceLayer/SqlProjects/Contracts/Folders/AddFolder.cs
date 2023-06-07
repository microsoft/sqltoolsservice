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
    /// Parameters for adding, deleting, or excluding a folder
    /// </summary>
    public class FolderParams : SqlProjectParams
    {
        /// <summary>
        /// Path of the folder, typically relative to the .sqlproj file
        /// </summary>
        public string Path { get; set; }
    }

    /// <summary>
    /// Add a folder to a project
    /// </summary>
    public class AddFolderRequest
    {
        public static readonly RequestType<FolderParams, ResultStatus> Type = RequestType<FolderParams, ResultStatus>.Create("sqlProjects/addFolder");
    }
}
