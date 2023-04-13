//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{    
    /// <summary>
    /// Create app role parameters
    /// </summary>
    public class CreateAppRoleParams
    {
        public string? ContextId { get; set; }

        public AppRoleInfo? AppRole { get; set; }
    }

    /// <summary>
    /// Create app role request type
    /// </summary>
    public class CreateAppRoleRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateAppRoleParams, object> Type =
            RequestType<CreateAppRoleParams, object>.Create("objectManagement/createAppRole");
    }

    /// <summary>
    /// Update app role params
    /// </summary>
    public class UpdateAppRoleParams
    {
        public string? ContextId { get; set; }

        public AppRoleInfo? AppRole { get; set; }
    }

    /// <summary>
    /// Update app role request type
    /// </summary>
    public class UpdateAppRoleRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAppRoleParams, object> Type =
            RequestType<UpdateAppRoleParams, object>.Create("objectManagement/updateAppRole");
    }


    /// <summary>
    /// Dispose app role params
    /// </summary>
    public class DisposeAppRoleViewRequestParams
    {
        public string? ContextId { get; set; }
    }

    /// <summary>
    /// Dispose app role view request type
    /// </summary>
    public class DisposeAppRoleViewRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DisposeLoginViewRequestParams, object> Type =
            RequestType<DisposeLoginViewRequestParams, object>.Create("objectManagement/disposeAppRoleView");
    }

    /// <summary>
    /// Initialize app role View Request params
    /// </summary>

    public class InitializeAppRoleViewRequestParams
    {
        public string? ConnectionUri { get; set; }
        public string? ContextId { get; set; }
        public bool IsNewObject { get; set; }

        public string? Name { get; set; }
    }

    /// <summary>
    /// Initialize app role View request type
    /// </summary>
    public class InitializeAppRoleViewRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<InitializeAppRoleViewRequestParams, AppRoleViewInfo> Type =
            RequestType<InitializeAppRoleViewRequestParams, AppRoleViewInfo>.Create("objectManagement/initializeAppRoleView");
    }
}
