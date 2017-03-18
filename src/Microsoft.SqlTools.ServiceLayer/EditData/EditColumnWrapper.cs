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
    public class EditColumnMetadata
    {
        #region Base Properties (properties provided by SMO)

        /// <summary>
        /// If set, this is a string representation of the default value. If set to null, then the
        /// column does not have a default value.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Escaped identifier for the name of the column
        /// </summary>
        public string EscapedName { get; set; }

        /// <summary>
        /// Whether or not the column is computed
        /// </summary>
        public bool IsComputed { get; set; }

        /// <summary>
        /// Whether or not the column is deterministically computed
        /// </summary>
        public bool IsDeterministic { get; set; }

        /// <summary>
        /// Whether or not the column is an identity column
        /// </summary>
        public bool IsIdentity { get; set; }

        /// <summary>
        /// The ordinal ID of the column
        /// </summary>
        public int Ordinal { get; set; }

        #endregion

        #region Extended Properties (properties provided by SqlClient)

        /// <summary>
        /// The DB column
        /// </summary>
        public DbColumnWrapper DbColumn { get; set; }

        /// <summary>
        /// Whether or not the column has extended properties
        /// </summary>
        public bool HasExtendedProperties { get; set; }

        /// <summary>
        /// Whether or not the column is calculated on the server side. This could be a computed
        /// column or a identity column.
        /// </summary>
        public bool? IsCalculated { get; set; }

        /// <summary>
        /// Whether or not the column is used in a key to uniquely identify a row
        /// </summary>
        public bool? IsKey { get; set; }

        /// <summary>
        /// Whether or not the column can be trusted for uniqueness
        /// </summary>
        public bool? IsTrustworthyForUniqueness { get; set; }

        #endregion
    }
}
