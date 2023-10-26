//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Migration.Tde.Validations;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Migration.Contracts
{
    public class TdeValidationParams
    {
        /// <summary>
        /// Source connection string for SQL Server.
        /// </summary>
        public string? SourceSqlConnectionString { get; set; }

        /// <summary>
        /// Location where certificates will be exported.
        /// </summary>
        public string? NetworkSharePath { get; set; }
    }

    public class TdeValidationRequest
    {
        public static readonly RequestType<TdeValidationParams, TdeValidationResult[]> Type =
            RequestType<TdeValidationParams, TdeValidationResult[]>.Create("migration/tdevalidation");
    }
}
