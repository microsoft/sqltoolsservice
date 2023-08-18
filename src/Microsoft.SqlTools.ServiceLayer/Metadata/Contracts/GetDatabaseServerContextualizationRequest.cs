//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GetDatabaseServerContextualizationParams
    {
        /// <summary>
        /// The URI of the connection to generate scripts for.
        /// </summary>
        public string OwnerUri { get; set; }
    }

    public class GetDatabaseServerContextualizationResult
    {
        /// <summary>
        /// An array containing generated create scripts for database objects like tables and views,
        /// </summary>
        public string[] Scripts { get; set; }
    }

    public class GetDatabaseServerContextualizationRequest
    {
        public static readonly RequestType<GetDatabaseServerContextualizationParams, GetDatabaseServerContextualizationResult> Type =
            RequestType<GetDatabaseServerContextualizationParams, GetDatabaseServerContextualizationResult>.Create("metadata/getDatabaseServerContextScripts");
    }
}
