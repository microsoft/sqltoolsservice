//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for filtering a the rows in a table to make querying easier
    /// </summary>
    public class EditInitializeFiltering
    {
        /// <summary>
        /// Limit the records queried from the database to this many. If null, all rows are returned
        /// </summary>
        public int? LimitResults { get; set; }
    }
}