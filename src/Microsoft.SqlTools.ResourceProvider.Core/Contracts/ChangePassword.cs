//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ResourceProvider.Core.Contracts
{
    /// <summary>
    /// A request to change the password of a SQL connection.
    /// </summary>
    public class ChangePasswordRequest
    {
        public static readonly
            RequestType<ChangePasswordParams, PasswordChangeResponse> Type =
            RequestType<ChangePasswordParams, PasswordChangeResponse>.Create("resource/changePassword");
    }

    /// <summary>
    /// Parameters for the Change Password Request.
    /// </summary>
    public class ChangePasswordParams : ConnectParams
    {
        /// <summary>
        /// The password to change the account of the connection to.
        /// </summary>
        public string NewPassword { get; set; }

        /// <summary>
        /// The connection type, for example MSSQL
        /// </summary>
        public string ConnectionTypeId { get; set; }
    }

    /// <summary>
    /// Parameters to be sent back after a password change attempt.
    /// </summary>
    public class PasswordChangeResponse
    {
        /// <summary>
        /// Status indicating if password change was successful or not.     
        /// </summary>
        public bool Result { get; set;  }

        /// <summary>
        /// Error message for the password change, if an error occured.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}