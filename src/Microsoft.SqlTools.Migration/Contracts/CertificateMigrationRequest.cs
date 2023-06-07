//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Migration.Contracts
{
    /// <summary>
    /// Parameters for the certificate migration operation
    /// </summary>
    public class CertificateMigrationParams
    {
        /// <summary>
        /// List of databses to migrate the certificates
        /// </summary>
        public List<string> EncryptedDatabases { get; set; } = new List<string>();

        /// <summary>
        /// Source connection string to the server
        /// </summary>
        public string SourceSqlConnectionString { get; set; }

        /// <summary>
        /// Target subscription id
        /// </summary>
        public string TargetSubscriptionId { get; set; }

        /// <summary>
        /// Target resource group name
        /// </summary>
        public string TargetResourceGroupName { get; set; }

        /// <summary>
        /// Target manages instance name
        /// </summary>
        public string TargetManagedInstanceName { get; set; }

        /// <summary>
        /// Place where certificates will be exported
        /// </summary>
        public string NetworkSharePath { get; set; }

        /// <summary>
        /// Domain for the user credentials able to read from the shared path
        /// </summary>
        public string NetworkShareDomain { get; set; }

        /// <summary>
        /// Username for the credentials able to read from the shared path
        /// </summary>
        public string NetworkShareUserName { get; set; }

        /// <summary>
        /// Password for the credentials able to read from the shared path
        /// </summary>
        public string NetworkSharePassword { get; set; } 
        
        /// <summary>
        /// Access token for the ARM client
        /// </summary>
        public string AccessToken { get; set; }
    }

    /// <summary>
    /// Result for the certificate migration operation
    /// </summary>
    public class CertificateMigrationResult
    {
        /// <summary>
        /// List of the status of each certificate migration result attempted.
        /// </summary>
        public List<CertificateMigrationEntryResult> MigrationStatuses { get; set; } = new List<CertificateMigrationEntryResult>();
    }

    /// <summary>
    /// Result for an individual database certificate migration
    /// </summary>
    public class CertificateMigrationEntryResult
    {
        /// <summary>
        /// The name of the database this result represent
        /// </summary>
        public string DbName { get; set; }

        /// <summary>
        /// The result of the migration.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Description of the success status or the error message encountered when the migration was not successful.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Status code returned by migration.
        /// </summary>
        public string StatusCode { get; set; }
    }

    /// <summary>
    /// Certificate migration request definition
    /// </summary>
    public class CertificateMigrationRequest
    {
        /// <summary>
        /// Name, parameter and return type for the certicate migration operation
        /// </summary>
        public static readonly
            RequestType<CertificateMigrationParams, CertificateMigrationResult> Type =
                RequestType<CertificateMigrationParams, CertificateMigrationResult>.Create("migration/tdemigration");
    }
}
