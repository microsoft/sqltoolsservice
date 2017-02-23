//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// An interface used in edit scenarios that defines properties for what columns are primary
    /// keys, and other metadata of the table.
    /// </summary>
    public interface IEditTableMetadata
    {
        /// <summary>
        /// All columns in the table that's being edited
        /// </summary>
        IEnumerable<EditColumnWrapper> Columns { get; }

        /// <summary>
        /// The escaped name of the table that's being edited
        /// </summary>
        string EscapedMultipartName { get; }

        /// <summary>
        /// Whether or not this table is a memory optimized table
        /// </summary>
        bool IsMemoryOptimized { get; }

        /// <summary>
        /// Columns that can be used to uniquely identify the a row
        /// </summary>
        IEnumerable<EditColumnWrapper> KeyColumns { get; }
    }
}
