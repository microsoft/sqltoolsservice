//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Migration.Logins.Contracts.Exceptions;
using Microsoft.SqlServer.Migration.Logins.Helpers;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.Migration.Contracts
{
    /// <summary>
    /// Represents the steps in pre validation.
    /// </summary>
    public enum LoginMigrationPreValidationStep
    {
        /// <summary>
        /// Run Sys Admin validations
        /// </summary>
        SysAdminValidation,

        /// <summary>
        /// Run AAD Domain name validations
        /// </summary>
        AADDomainNameValidation,

        /// <summary>
        /// Run User Mapping validations
        /// </summary>
        UserMappingValidation,

        /// <summary>
        /// Run Login Eligibility validations
        /// </summary>
        LoginEligibilityValidation,
    }

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

    /// <summary>
    /// Represents the parameters for start login migration.
    /// </summary>
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
        /// Microsoft Entra domain name (required for Windows Auth)
        /// </summary>
        public string AADDomainName{ get; set; }
    }

    /// <summary>
    /// Represents the contents of the Login migration result.
    /// </summary>
    public class LoginMigrationResult
    {
        /// <summary>
        /// Exceptions per logins
        /// </summary>
        public IDictionary<string, IEnumerable<LoginMigrationException>> ExceptionMap { get; set; }

        /// <summary>
        /// The login migration step that just completed
        /// </summary>
        public LoginMigrationStep CompletedStep { get; set; }

        /// <summary>
        /// How long this step took
        /// </summary>
        public string ElapsedTime{ get; set; }
    }

    /// <summary>
    /// Represents the contents of the Login migration pre validation result.
    /// </summary>
    public class LoginMigrationPreValidationResult
    {
        /// <summary>
        /// Exceptions per logins
        /// </summary>
        public IDictionary<string, IEnumerable<LoginMigrationException>> ExceptionMap { get; set; }

        /// <summary>
        /// The login migration pre validation step that just completed
        /// </summary>
        public LoginMigrationPreValidationStep CompletedStep { get; set; }

        /// <summary>
        /// How long this step took
        /// </summary>
        public string ElapsedTime{ get; set; }
    }

    /// <summary>
    /// Defines a request sent from the client to start login migration.
    /// </summary>
    public class StartLoginMigrationRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/startloginmigration");
    }

    /// <summary>
    /// Defines a request sent from the client to validate login migration.
    /// </summary>
    public class ValidateLoginMigrationRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/validateloginmigration");
    }

    /// <summary>
    /// Defines a request sent from the client to validate Sys Admin Permission.
    /// </summary>
    public class ValidateSysAdminPermissionRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationPreValidationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationPreValidationResult>.Create("migration/validatesysadminpermission");
    }

    /// <summary>
    /// Defines a request sent from the client to validate user mapping.
    /// </summary>
    public class ValidateUserMappingRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationPreValidationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationPreValidationResult>.Create("migration/validateusermapping");
    }

    /// <summary>
    /// Defines a request sent from the client to validate AAD domain name.
    /// </summary>
    public class ValidateAADDomainNameRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationPreValidationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationPreValidationResult>.Create("migration/validateaaddomainname");
    }

    /// <summary>
    /// Defines a request sent from the client to validate Login Eligibility.
    /// </summary>
    public class ValidateLoginEligibilityRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationPreValidationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationPreValidationResult>.Create("migration/validatelogineligibility");
    }

    /// <summary>
    /// Defines a request sent from the client to migrate logins.
    /// </summary>
    public class MigrateLoginsRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/migratelogins");
    }

    /// <summary>
    /// Defines a request sent from the client to establish user mapping.
    /// </summary>
    public class EstablishUserMappingRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/establishusermapping");
    }

    /// <summary>
    /// Defines a request sent from the client to migrate server roles and set permissions.
    /// </summary>
    public class MigrateServerRolesAndSetPermissionsRequest
    {
        public static readonly
            RequestType<StartLoginMigrationParams, LoginMigrationResult> Type =
                RequestType<StartLoginMigrationParams, LoginMigrationResult>.Create("migration/migrateserverrolesandsetpermissions");
    }

    /// <summary>
    /// Defines a notification sent to the client to provide progress notification of the login migration.
    /// </summary>
    public class LoginMigrationNotification
    {
        public static readonly
            EventType<LoginMigrationResult> Type =
            EventType<LoginMigrationResult>.Create("migration/loginmigrationnotification");
    }

    /// <summary>
    /// Defines an progress notification (for individual login) sent to the client.
    /// </summary>
    public class LoginMigrationProgressNotificationEvent
    {
        public static readonly
            EventType<LoginMigrationProgressNotification> Type =
            EventType<LoginMigrationProgressNotification>.Create("migration/loginmigrationprogressnotification");
    }
}