//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parameters for the Change Password Request.
    /// </summary>
    public class ChangePasswordParams : ConnectParams
    {
        /// <summary>
        /// The password to change the account of the connection to.
        /// </summary>
        public string NewPassword { get; set; }
    }
}
