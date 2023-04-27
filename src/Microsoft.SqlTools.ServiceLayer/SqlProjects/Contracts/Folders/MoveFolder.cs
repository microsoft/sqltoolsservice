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
    /// Parameters for moving a folder
    /// </summary>
    public class MoveFolderParams : FolderParams
    {
        /// <summary>
        /// Path of the folder, typically relative to the .sqlproj file
        /// </summary>
        public string DestinationPath { get; set; }
    }

    /// <summary>
    /// Move a folder and its contents within a project
    /// </summary>
    public class MoveFolderRequest
    {
        public static readonly RequestType<MoveFolderParams, ResultStatus> Type = RequestType<MoveFolderParams, ResultStatus>.Create("sqlProjects/moveFolder");
    }
}
