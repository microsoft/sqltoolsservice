//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerTable
    {
        /// <summary>
        /// Gets or sets the unique identifier for the table
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Gets or sets the name of the table
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Gets or sets the schema of the table
        /// </summary>
        public string? Schema { get; set; }
        /// <summary>
        /// Gets or sets the columns in the table
        /// </summary>
        public List<SchemaDesignerColumn>? Columns { get; set; }
        /// <summary>
        /// Gets or sets the foreign keys in the table
        /// </summary>
        public List<SchemaDesignerForeignKey>? ForeignKeys { get; set; }
    }
}