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
    /// Parameters for setting the DatabaseSchemaProvider property of a SQL project
    /// </summary>
    public class SetDatabaseSchemaProviderParams : SqlProjectParams
    {
        /// <summary>
        /// Source of the database schema, used in telemetry
        /// </summary>
        public string DatabaseSchemaProvider { get; set; }
    }

    /// <summary>
    /// Set the DatabaseSchemaProvider property of a SQL project
    /// </summary>
    public class SetDatabaseSchemaProviderRequest
    {
        public static readonly RequestType<SetDatabaseSchemaProviderParams, ResultStatus> Type = RequestType<SetDatabaseSchemaProviderParams, ResultStatus>.Create("sqlProjects/setDatabaseSchemaProvider");
    }
}
