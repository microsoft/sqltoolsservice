//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{
    /// <summary>
    /// Initialize User View parameters
    /// </summary>
    public class InitializeUserViewParams
    {
        public string? ContextId { get; set; }

        public string? ConnectionUri { get; set; }

        public bool IsNewObject { get; set; }

        public string? Database { get; set; }

        public string? Name { get; set; }
    }

    /// <summary>
    /// Initialize User View request type
    /// </summary>
    public class InitializeUserViewRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<InitializeUserViewParams, UserViewInfo> Type =
            RequestType<InitializeUserViewParams, UserViewInfo>.Create("objectManagement/initializeUserView");
    }

    /// <summary>
    /// Create User parameters
    /// </summary>
    public class CreateUserParams
    {
        public string? ContextId { get; set; }
        public UserInfo? User { get; set; }
    }

    /// <summary>
    /// Create User result
    /// </summary>
    public class CreateUserResult : ResultStatus
    {
        public UserInfo? User { get; set; }
    }

    /// <summary>
    /// Create User request type
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateUserParams, CreateUserResult> Type =
            RequestType<CreateUserParams, CreateUserResult>.Create("objectManagement/createUser");
    }

    /// <summary>
    /// Update User parameters
    /// </summary>
    public class UpdateUserParams
    {
        public string? ContextId { get; set; }
        public UserInfo? User { get; set; }
    }

    /// <summary>
    /// Update User request type
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateUserParams, ResultStatus> Type =
            RequestType<UpdateUserParams, ResultStatus>.Create("objectManagement/updateUser");
    }

    /// <summary>
    /// Delete User params
    /// </summary>
    public class DeleteUserParams
    {
        public string? ConnectionUri { get; set; }
	    
        public string? Database { get; set; }
	
        public string? Name { get; set; }
    }

    /// <summary>
    /// Delete User request type
    /// </summary>
    public class DeleteUserRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteUserParams, ResultStatus> Type =
            RequestType<DeleteUserParams, ResultStatus>.Create("objectManagement/deleteUser");
    }

    /// <summary>
    /// Update User params
    /// </summary>
    public class DisposeUserViewRequestParams
    {
        public string? ContextId { get; set; }
    }

    /// <summary>
    /// Update User request type
    /// </summary>
    public class DisposeUserViewRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DisposeUserViewRequestParams, ResultStatus> Type =
            RequestType<DisposeUserViewRequestParams, ResultStatus>.Create("objectManagement/disposeUserView");
    }
}
