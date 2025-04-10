//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerForeignKey
    {
        /// <summary>
        /// Gets or sets the unique identifier for the relationship
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Gets or sets the name of the foreign key
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Gets or sets the parent columns
        /// </summary>
        public List<string>? Columns { get; set; }
        /// <summary>
        /// Gets or sets the referenced schema
        /// </summary>
        public string? ReferencedSchemaName { get; set; }
        /// <summary>
        /// Gets or sets the referenced entity (Table in MSSQL) name.
        /// </summary>
        public string? ReferencedTableName { get; set; }
        /// <summary>
        /// Gets or sets the referenced column
        /// </summary>
        public List<string>? ReferencedColumns { get; set; }
        /// <summary>
        /// Gets or sets the delete cascade action. Default is NO_ACTION
        /// </summary>
        public OnAction OnDeleteAction { get; set; }
        /// <summary>
        /// Gets or sets the update cascade action. Default is NO_ACTION
        /// </summary>
        public OnAction OnUpdateAction { get; set; }
    }

    public enum OnAction
    {
        /// <summary>
        /// Cascade action. Delete or update the row from the parent table and automatically delete or update the matching rows in the child table.
        /// </summary>
        CASCADE = 0,
        /// <summary>
        /// No action. Do not allow the delete or update of the row from the parent table if there are matching rows in the child table.
        /// </summary>
        NO_ACTION = 1,
        /// <summary>
        /// Set null action. Delete or update the row from the parent table and set the foreign key column or columns in the child table to NULL.
        /// </summary>
        SET_NULL = 2,
        /// <summary>
        /// Set default action. Delete or update the row from the parent table and set the foreign key column or columns in the child table to their default values.
        /// </summary>
        SET_DEFAULT = 3
    }
}