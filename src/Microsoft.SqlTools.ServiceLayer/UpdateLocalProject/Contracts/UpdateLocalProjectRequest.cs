//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.UpdateLocalProject.Contracts
{
    /// <summary>
    /// Parameters for an update local project request
    /// </summary>
    public class UpdateLocalProjectParams
    {
        /// <summary>
        /// Gets or sets the file structure of the local project
        /// </summary>
        public string FolderStructure { get; set; }

        /// <summary>
        /// Gets or sets the database of the local project
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the path of the local project
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the owner uri of the database
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Gets or sets the DSP version of the database
        /// </summary>
        public string Dsp { get; set; }
    }

    public class UpdateLocalProjectResult : ResultStatus
    {
        /// <summary>
        /// Files that were added by operation
        /// </summary>
        public string[] AddedFiles { get; set; }

        /// <summary>
        /// Files that were deleted by operation
        /// </summary>
        public string[] DeletedFiles { get; set; }

        /// <summary>
        /// Files that were changed by operation
        /// </summary>
        public string[] ChangedFiles { get; set; }
    }

    /// <summary>
    /// Defines the update local project request type
    /// </summary>
    class UpdateLocalProjectRequest
    {
        public static readonly RequestType<UpdateLocalProjectParams, UpdateLocalProjectResult> Type =
            RequestType<UpdateLocalProjectParams, UpdateLocalProjectResult>.Create("updatelocalproject");
    }
}
