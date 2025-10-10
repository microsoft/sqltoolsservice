//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Metadata about a column in an editable result set
    /// </summary>
    public class EditColumnInfo
    {
        /// <summary>
        /// The name of the column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether or not the column can be edited
        /// </summary>
        public bool IsEditable { get; set; }
    }
}
