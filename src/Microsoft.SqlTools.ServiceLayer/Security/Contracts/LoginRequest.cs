//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
        public string OwnerUri { get; set; }

        public LoginInfo Login { get; set; }
    }

    /// <summary>
    /// Create Login result
    /// </summary>
    public class CreateLoginResult : ResultStatus
    {
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
            RequestType<CreateLoginParams, CreateLoginResult> Type =
            RequestType<CreateLoginParams, CreateLoginResult>.Create("security/createlogin");
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
            RequestType<DeleteLoginParams, ResultStatus>.Create("security/deletelogin");
    }
}
