//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Small class that stores information needed by the edit data service to properly process
    /// edits into scripts.
    /// </summary>
    public class EditColumnWrapper
    {
        /// <summary>
        /// The DB column
        /// </summary>
        public DbColumnWrapper DbColumn { get; set; }

        /// <summary>
        /// Escaped identifier for the name of the column
        /// </summary>
        public string EscapedName { get; set; }

        /// <summary>
        /// Whether or not the column is used in a key to uniquely identify a row
        /// </summary>
        public bool IsKey { get; set; }

        /// <summary>
        /// Whether or not the column can be trusted for uniqueness
        /// </summary>
        public bool IsTrustworthyForUniqueness { get; set; }

        /// <summary>
        /// The ordinal ID of the column
        /// </summary>
        public int Ordinal { get; set; }
    }
}
