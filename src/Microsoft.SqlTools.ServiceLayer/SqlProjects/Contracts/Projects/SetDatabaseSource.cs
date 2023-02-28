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
    /// Parameters for setting the DatabaseSource property of a .sqlproj file
    /// </summary>
    public class SetDatabaseSourceParams : SqlProjectParams
    {
        /// <summary>
        /// Source of the database schema, used in telemetry
        /// </summary>
        public string DatabaseSource { get; set; }
    }

    /// <summary>
    /// Set the DatabaseSource property of a .sqlproj file
    /// </summary>
    public class SetDatabaseSourceRequest
    {
        public static readonly RequestType<SetDatabaseSourceParams, ResultStatus> Type = RequestType<SetDatabaseSourceParams, ResultStatus>.Create("sqlProjects/setDatabaseSource");
    }
}
