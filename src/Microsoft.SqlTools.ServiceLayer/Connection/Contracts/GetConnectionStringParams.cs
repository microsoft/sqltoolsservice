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
    }
}
