//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Get the cross-platform compatibility status for a project
    /// </summary>
    public class GetProjectPropertiesRequest
    {
        public static readonly RequestType<SqlProjectParams, GetProjectPropertiesResult> Type = RequestType<SqlProjectParams, GetProjectPropertiesResult>.Create("sqlProjects/getProjectProperties");
    }

    /// <summary>
    /// Result containing project properties contained in the .sqlproj XML
    /// </summary>
    public class GetProjectPropertiesResult : ResultStatus
    {
        /// <summary>
        /// GUID for the SQL project
        /// </summary>
        public string ProjectGuid { get; set; }

        /// <summary>
        /// Build configuration, defaulted to Debug if not specified
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Build platform, defaulted to AnyCPU if not specified
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Output path for build, defaulted to "bin/Debug" if not specified.
        /// May be absolute or relative.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Default collation for the project, defaulted to SQL_Latin1_General_CP1_CI_AS if not specified
        /// </summary>
        public string DefaultCollation { get; set; }

        /// <summary>
        /// Source of the database schema, used in telemetry
        /// </summary>
        public string? DatabaseSource { get; set; }

        /// <summary>
        /// Style of the .sqlproj file - SdkStyle or LegacyStyle
        /// </summary>
        public ProjectType ProjectStyle { get; set; }
    }
}
