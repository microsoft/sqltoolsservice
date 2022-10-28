//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
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
    }

    public class CertificateMigrationResult
    {
        /// <summary>
        /// List of the status of each certificate migration result attempted.
        /// </summary>
        public List<CertificateMigrationEntryResult> MigrationStatuses { get; set; } = new List<CertificateMigrationEntryResult>();
    }

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
        /// Description of the error message encounter when the migratio was not successful
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Create a certificate migration request
    /// </summary>
    public class CertificateMigrationRequest
    {
        public static readonly
            RequestType<CertificateMigrationParams, CertificateMigrationResult> Type =
                RequestType<CertificateMigrationParams, CertificateMigrationResult>.Create("migration/tdemigration");
    }
}
