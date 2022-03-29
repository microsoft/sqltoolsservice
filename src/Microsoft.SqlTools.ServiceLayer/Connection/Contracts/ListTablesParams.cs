//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parameters for the List Tables Request.
    /// </summary>
    public class ListTablesParams
    {
        /// <summary>
        /// URI of the owner of the connection requesting the list of tables.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// whether to include the details of the tables.
        /// </summary>
        public bool? IncludeDetails { get; set; }
    }
}
