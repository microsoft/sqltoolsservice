//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{    
    /// <summary>
    /// Create database role parameters
    /// </summary>
    public class CreateDatabaseRoleParams
    {
        public string? ContextId { get; set; }

        public DatabaseRoleInfo? DatabaseRole { get; set; }
    }

    /// <summary>
    /// Create database role request type
    /// </summary>
    public class CreateDatabaseRoleRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateDatabaseRoleParams, object> Type =
            RequestType<CreateDatabaseRoleParams, object>.Create("objectManagement/createDatabaseRole");
    }

    /// <summary>
    /// Update database role params
    /// </summary>
    public class UpdateDatabaseRoleParams
    {
        public string? ContextId { get; set; }

        public DatabaseRoleInfo? DatabaseRole { get; set; }
    }

    /// <summary>
    /// Update database role request type
    /// </summary>
    public class UpdateDatabaseRoleRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateDatabaseRoleParams, object> Type =
            RequestType<UpdateDatabaseRoleParams, object>.Create("objectManagement/updateDatabaseRole");
    }


    /// <summary>
    /// Dispose database role params
    /// </summary>
    public class DisposeDatabaseRoleViewRequestParams
    {
        public string? ContextId { get; set; }
    }

    /// <summary>
    /// Dispose database role view request type
    /// </summary>
    public class DisposeDatabaseRoleViewRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DisposeLoginViewRequestParams, object> Type =
            RequestType<DisposeLoginViewRequestParams, object>.Create("objectManagement/disposeDatabaseRoleView");
    }

    /// <summary>
    /// Initialize database role View Request params
    /// </summary>

    public class InitializeDatabaseRoleViewRequestParams
    {
        public string? ConnectionUri { get; set; }
        public string? ContextId { get; set; }
        public bool IsNewObject { get; set; }

        public string? Name { get; set; }
    }

    /// <summary>
    /// Initialize database role View request type
    /// </summary>
    public class InitializeDatabaseRoleViewRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<InitializeDatabaseRoleViewRequestParams, DatabaseRoleViewInfo> Type =
            RequestType<InitializeDatabaseRoleViewRequestParams, DatabaseRoleViewInfo>.Create("objectManagement/initializeDatabaseRoleView");
    }
}
