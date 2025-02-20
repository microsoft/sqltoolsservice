//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    /// <summary>
    /// Represents a column in an entity
    /// </summary>
    public class IColumn
    {
        /// <summary>
        /// Gets or sets the unique identifier for the column
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Gets or sets the name of the column
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the data type of the column
        /// </summary>
        public string DataType { get; set; }
        /// <summary>
        /// Gets or sets if the column is a primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }
        /// <summary>
        /// Gets or sets if the column is an identity column
        /// </summary>
        public bool IsIdentity { get; set; }
    }
}