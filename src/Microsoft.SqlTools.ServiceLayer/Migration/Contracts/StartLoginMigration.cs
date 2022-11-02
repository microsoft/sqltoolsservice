//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.DataCollection.Common.Contracts.OperationsInfrastructure;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class StartLoginMigrationParams
    {
        /// <summary>
        /// Connection string to connect to source 
        /// </summary>
        public string SourceConnectionString { get; set; }

        /// <summary>
        /// Connection string to connect to target
        /// </summary>
        public string TargetConnectionString { get; set; }

        /// <summary>
        /// List of logins to migrate
        /// </summary>
        public List<string> LoginList { get; set; }

        /// <summary>
        /// Azure active directory domain name (required for Windows Auth)
        /// </summary>
        public string AADDomainName{ get; set; }
    }

    public class StartLoginMigrationResults
    {
        /// <summary>
        /// Start time of the assessment
        /// </summary>
        public IDictionary<string, IEnumerable<ReportableException>> ExceptionMap { get; set; }
    }

    public class StartLoginMigrationRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, StartLoginMigrationResults> Type =
                RequestType<StartLoginMigrationParams, StartLoginMigrationResults>.Create("migration/startloginmigration");
    }
}