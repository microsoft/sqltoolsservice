//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parameters to be sent back after a password change attempt.
    /// </summary>
    public class PasswordChangeResponse
    {
        /// <summary>
        /// Status indicating if connection was successful or not.     
        /// </summary>
        public bool Result { get; set;  }

        /// <summary>
        /// Error message for the connection failure, if an error occured.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
