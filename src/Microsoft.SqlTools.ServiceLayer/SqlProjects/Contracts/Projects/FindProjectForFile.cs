//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Find the SQL project that owns a .sql file, without scanning the workspace
    /// </summary>
    public class FindProjectForFileRequest
    {
        public static readonly RequestType<FindProjectForFileParams, FindProjectForFileResult> Type = RequestType<FindProjectForFileParams, FindProjectForFileResult>.Create("sqlProjects/findProjectForFile");
    }

    public class FindProjectForFileParams : GeneralRequestDetails
    {
        /// <summary>
        /// Absolute path of the file to look up
        /// </summary>
        public string FilePath { get; set; }
    }

    /// <summary>
    /// Result containing the project that owns the file, if any
    /// </summary>
    public class FindProjectForFileResult : ResultStatus
    {
        /// <summary>
        /// Absolute path of the owning .sqlproj, or null when the file is not in a project
        /// </summary>
        public string ProjectUri { get; set; }

        /// <summary>
        /// Whether the owning project is currently loaded in the service
        /// </summary>
        public bool IsLoaded { get; set; }
    }
}
