﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for creating a new SQL project
    /// </summary>
    public class CreateSqlProjectParams : SqlProjectParams
    {
        /// <summary>
        /// Type of SQL Project: SDK-style or Legacy
        /// </summary>
        public ProjectType SqlProjectType { get; set; }

        /// <summary>
        /// Database schema provider for the project, in the format
        /// "Microsoft.Data.Tools.Schema.Sql.SqlXYZDatabaseSchemaProvider".
        /// Case sensitive.
        /// </summary>
        public string? DatabaseSchemaProvider { get; set; }

        /// <summary>
        /// Version of the Microsoft.Build.Sql SDK for the project, if overriding the default
        /// </summary>
        public string? BuildSdkVersion { get; set; }
    }

    /// <summary>
    /// Create a new SQL project
    /// </summary>
    public class CreateSqlProjectRequest
    {
        public static readonly RequestType<CreateSqlProjectParams, ResultStatus> Type = RequestType<CreateSqlProjectParams, ResultStatus>.Create("sqlProjects/createProject");
    }
}
