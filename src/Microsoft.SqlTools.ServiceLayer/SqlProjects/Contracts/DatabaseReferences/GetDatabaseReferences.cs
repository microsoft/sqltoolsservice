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
    public class GetDatabaseReferencesRequest
    {
        public static readonly RequestType<SqlProjectParams, GetDatabaseReferencesResult> Type = RequestType<SqlProjectParams, GetDatabaseReferencesResult>.Create("sqlProjects/getDatabaseReferences");
    }

    /// <summary>
    /// Result containing database references in the project
    /// </summary>
    public class GetDatabaseReferencesResult : ResultStatus
    {
        /// <summary>
        /// Array of system database references contained in the project
        /// </summary>
        public SystemDatabaseReference[] SystemDatabaseReferences { get; set; }

        /// <summary>
        /// Array of dacpac references contained in the project
        /// </summary>
        public DacpacReference[] DacpacReferences { get; set; }

        /// <summary>
        /// Array of SQL project references contained in the project
        /// </summary>
        public SqlProjectReference[] SqlProjectReferences { get; set; }
    }
}
