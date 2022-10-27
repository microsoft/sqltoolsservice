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

    public class SourceSqlConnectionEntry
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
    }

    public class CertificateMigrationResult
    {
        public List<CertificateMigrationEntryResult> MigrationStatuses { get; set; } = new List<CertificateMigrationEntryResult>();
    }

    public class CertificateMigrationEntryResult
    {
        public string DbName { get; set; }
        public bool Success { get; set; }

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
