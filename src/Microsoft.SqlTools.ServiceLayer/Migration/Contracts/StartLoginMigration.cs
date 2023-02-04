//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.DataCollection.Common.Contracts.OperationsInfrastructure;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    /// <summary>
    /// Represents the steps in login migration.
    /// </summary>
    public enum LoginMigrationStep
    {
        /// <summary>
        /// Run pre-migration validations 
        /// </summary>
        StartValidations,

        /// <summary>
        /// Step to hash passwords and migrate logins
        /// </summary>
        MigrateLogins,

        /// <summary>
        /// Step to establish users and logins from source to target
        /// </summary>
        EstablishUserMapping,


        /// <summary>
        /// Step to migrate server roles
        /// </summary>
        MigrateServerRoles,

        /// <summary>
        /// Step to establish roles
        /// </summary>
        EstablishServerRoleMapping,

        /// <summary>
        /// Step to map all the grant/deny permissions for logins
        /// </summary>
        SetLoginPermissions,

        /// <summary>
        /// Step to map all server roles grant/deny permissions
        /// </summary>
        SetServerRolePermissions
    }

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

    public class LoginMigrationResult
    {
        /// <summary>
        /// Start time of the assessment
        /// </summary>
        public IDictionary<string, IEnumerable<ReportableException>> ExceptionMap { get; set; }

        /// <summary>
        /// The login migration step that just completed
        /// </summary>
        public LoginMigrationStep CompletedStep { get; set; }

        /// <summary>
        /// How long this step took
        /// </summary>
        public string ElapsedTime{ get; set; }
    }

    public class StartLoginMigrationRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/startloginmigration");
    }

    public class ValidateLoginMigrationRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/validateloginmigration");
    }

    public class MigrateLoginsRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/migratelogins");
    }

    public class EstablishUserMappingRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/establishusermapping");
    }
    public class MigrateServerRolesAndSetPermissionsRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/migrateserverrolesandsetpermissions");
    }

    public class LoginMigrationNotification
    {
        public static readonly
            EventType<LoginMigrationResult> Type =
            EventType<LoginMigrationResult>.Create("migration/loginmigrationnotification");
    }
}