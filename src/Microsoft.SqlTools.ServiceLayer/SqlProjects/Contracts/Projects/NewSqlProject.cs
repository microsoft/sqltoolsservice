//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for creating a new SQL Project
    /// </summary>
    public class NewSqlProjectParams : SqlProjectParams
    {
        /// <summary>
        /// Type of SQL Project: SDK-style or Legacy
        /// </summary>
        public ProjectType SqlProjectType { get; set; }

        /// <summary>
        /// The database schema provider for the project, in the format
        /// "Microsoft.Data.Tools.Schema.Sql.SqlXYZDatabaseSchemaProvider".
        /// Case sensitive.
        /// </summary>
        public string? DatabaseSchemaProvider { get; set; }
    }

    public class NewSqlProjectRequest
    {
        public static readonly RequestType<NewSqlProjectParams, SqlProjectResult> Type = RequestType<NewSqlProjectParams, SqlProjectResult>.Create("sqlprojects/newProject");
    }
}
