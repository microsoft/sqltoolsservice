//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Metadata
{
    /// <summary>
    /// ColumnMetadata class
    /// </summary>
    public class ColumnMetadata
    {
        /// <summary>
        /// Constructs a simple edit column metadata provider
        /// </summary>
        public ColumnMetadata()
        {
            HasExtendedProperties = false;
        }

        #region Basic Properties (properties provided by SMO)

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

        // public DbColumnWrapper DbColumn { get; private set; }

        /// <summary>
        /// Whether or not the column has extended properties
        /// </summary>
        public bool HasExtendedProperties { get; private set; }

        /// <summary>
        /// Whether or not the column is calculated on the server side. This could be a computed
        /// column or a identity column.
        /// </summary>
        public bool? IsCalculated { get; private set; }

        /// <summary>
        /// Whether or not the column is used in a key to uniquely identify a row
        /// </summary>
        public bool? IsKey { get; private set; }

        /// <summary>
        /// Whether or not the column can be trusted for uniqueness
        /// </summary>
        public bool? IsTrustworthyForUniqueness { get; private set; }

        #endregion

        /// <summary>
        /// Extracts extended column properties from the database columns from SQL Client
        /// </summary>
        /// <param name="dbColumn">The column information provided by SQL Client</param>
        public void Extend(DbColumnWrapper dbColumn)
        {
            Validate.IsNotNull(nameof(dbColumn), dbColumn);

            // DbColumn = dbColumn;

            // A column is trustworthy for uniqueness if it can be updated or it has an identity
            // property. If both of these are false (eg, timestamp) we can't trust it to uniquely
            // identify a row in the table
            IsTrustworthyForUniqueness = dbColumn.IsUpdatable || dbColumn.IsIdentity.HasTrue();

            // A key column is determined by whether it is a key
            IsKey = dbColumn.IsKey;

            // A column is calculated if it is identity, computed, or otherwise not updatable
            IsCalculated = IsIdentity || IsComputed || !dbColumn.IsUpdatable;

            // Mark the column as extended
            HasExtendedProperties = true;
        }
    }
}
