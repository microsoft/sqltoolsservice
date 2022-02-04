//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parameters for the Get Connection String Request.
    /// </summary>
    public class GetConnectionStringParams
    {
        /// <summary>
        /// URI of the owner of the connection
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Connection information for the connection
        /// </summary>
        public ConnectionInfo ConnectionInfo { get; set; }

        /// <summary>
        /// Indicates whether the password should be return in the connection string
        /// default is set to false
        /// </summary>
        public bool IncludePassword { get; set; }

        /// <summary>
        /// Indicates whether the application name should be return in the connection string
        /// default is set to true
        /// </summary>
        public bool? IncludeApplicationName { get; set;}
    }
}
