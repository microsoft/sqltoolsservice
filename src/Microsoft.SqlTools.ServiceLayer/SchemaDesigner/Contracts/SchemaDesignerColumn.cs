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
    public class SchemaDesignerColumn
    {
        /// <summary>
        /// Gets or sets the unique identifier for the column
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Gets or sets the name of the column
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Gets or sets the data type of the column
        /// </summary>
        public string? DataType { get; set; }
        /// <summary>
        /// Gets or sets the max length of the column
        /// </summary>
        public string? MaxLength { get; set; }
        /// <summary>
        /// Gets or sets the precision of the column
        /// </summary>
        public int? Precision { get; set; }
        /// <summary>
        /// Gets or sets the scale of the column
        /// </summary>
        public int? Scale { get; set; }
        /// <summary>
        /// Gets or sets if the column is a primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }
        /// <summary>
        /// Gets or sets if the column is an identity column
        /// </summary>
        public bool IsIdentity { get; set; }
        /// <summary>
        /// Gets or sets identity seed of the column
        /// </summary>
        public decimal? IdentitySeed { get; set; }
        /// <summary>
        /// Gets or sets identity increment of the column
        /// </summary>
        public decimal? IdentityIncrement { get; set; }
        /// <summary>
        /// Gets or sets if the column is a nullable column
        /// </summary>
        public bool IsNullable { get; set; }
        /// <summary>
        /// Gets or sets the default value of the column
        /// </summary>
        public string? DefaultValue { get; set; }
    }
}