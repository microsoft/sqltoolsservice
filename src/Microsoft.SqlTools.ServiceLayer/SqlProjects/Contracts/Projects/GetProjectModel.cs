//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Get everything needed to display a project in one request: properties, SQLCMD variables,
    /// database references, and all items. Replaces calling each getter separately.
    /// </summary>
    public class GetProjectModelRequest
    {
        public static readonly RequestType<SqlProjectParams, GetProjectModelResult> Type = RequestType<SqlProjectParams, GetProjectModelResult>.Create("sqlProjects/getProjectModel");
    }

    /// <summary>
    /// Result containing the full contents of a project
    /// </summary>
    public class GetProjectModelResult : ResultStatus
    {
        /// <summary>
        /// Project properties, as returned by sqlProjects/getProjectProperties
        /// </summary>
        public GetProjectPropertiesResult Properties { get; set; }

        /// <summary>
        /// Whether the project is compatible with cross-platform builds
        /// </summary>
        public bool IsCrossPlatformCompatible { get; set; }

        /// <summary>
        /// SQLCMD variables contained in the project
        /// </summary>
        public SqlCmdVariable[] SqlCmdVariables { get; set; }

        /// <summary>
        /// Database references contained in the project, as returned by sqlProjects/getDatabaseReferences
        /// </summary>
        public GetDatabaseReferencesResult DatabaseReferences { get; set; }

        /// <summary>
        /// Relative paths of the SQL object scripts (Build items) in the project
        /// </summary>
        public string[] SqlObjectScripts { get; set; }

        /// <summary>
        /// Relative paths of the pre-deployment scripts in the project
        /// </summary>
        public string[] PreDeploymentScripts { get; set; }

        /// <summary>
        /// Relative paths of the post-deployment scripts in the project
        /// </summary>
        public string[] PostDeploymentScripts { get; set; }

        /// <summary>
        /// Relative paths of the None items in the project. Glob patterns are left out.
        /// </summary>
        public string[] NoneItems { get; set; }

        /// <summary>
        /// Relative paths of the folders in the project
        /// </summary>
        public string[] Folders { get; set; }
    }
}
