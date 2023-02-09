//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{    
    /// <summary>
    /// Create Login parameters
    /// </summary>
    public class CreateLoginParams : GeneralRequestDetails
    {
        public string ContextId { get; set; }

        public LoginInfo Login { get; set; }
    }

    /// <summary>
    /// Create Login request type
    /// </summary>
    public class CreateLoginRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateLoginParams, object> Type =
            RequestType<CreateLoginParams, object>.Create("objectManagement/createLogin");
    }

    /// <summary>
    /// Delete Login params
    /// </summary>
    public class DeleteLoginParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string LoginName { get; set; }
    }

    /// <summary>
    /// Delete Login request type
    /// </summary>
    public class DeleteLoginRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteLoginParams, ResultStatus> Type =
            RequestType<DeleteLoginParams, ResultStatus>.Create("objectManagement/deleteLogin");
    }

    /// <summary>
    /// Update Login params
    /// </summary>
    public class UpdateLoginParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string LoginName { get; set; }
    }

    /// <summary>
    /// Update Login request type
    /// </summary>
    public class UpdateLoginRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateLoginParams, ResultStatus> Type =
            RequestType<UpdateLoginParams, ResultStatus>.Create("objectManagement/updateLogin");
    }


    /// <summary>
    /// Update Login params
    /// </summary>
    public class DisposeLoginViewRequestParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string LoginName { get; set; }
    }

    /// <summary>
    /// Update Login request type
    /// </summary>
    public class DisposeLoginViewRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DisposeLoginViewRequestParams, ResultStatus> Type =
            RequestType<DisposeLoginViewRequestParams, ResultStatus>.Create("objectManagement/disposeLoginView");
    }

    /// <summary>
    /// Initialize Login View Request params
    /// </summary>

    public class InitializeLoginViewRequestParams : GeneralRequestDetails
    {
        public string ConnectionUri { get; set; }
        public string ContextId { get; set; }
        public bool IsNewObject { get; set; }

        public string Name { get; set; }
    }

    /// <summary>
    /// Initialize Login View request type
    /// </summary>
    public class InitializeLoginViewRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<InitializeLoginViewRequestParams, LoginViewInfo> Type =
            RequestType<InitializeLoginViewRequestParams, LoginViewInfo>.Create("objectManagement/initializeLoginView");
    }
}
