//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parameters to be sent back with a connection complete event
    /// </summary>
    public class PasswordChangeResponse
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public bool result { get; set;  }

        /// <summary>
        /// Additional optional detailed error messages, if an error occurred.
        /// </summary>
        public string? Messages { get; set; }

        /// <summary>
        /// Error message for the connection failure, if an error occured.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
