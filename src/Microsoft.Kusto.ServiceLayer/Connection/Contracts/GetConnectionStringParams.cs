//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
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
        /// Indicates whether the password should be return in the connection string
        /// </summary>
        public bool IncludePassword { get; set; }
    }
}
