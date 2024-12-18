//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class Relationship
    {
        /// <summary>
        /// Gets or sets the name of the foreign key
        /// </summary>
        public string ForeignKeyName { get; set; }
        /// <summary>
        /// Gets or sets the parent table
        /// </summary>
        public string Entity { get; set; }
        /// <summary>
        /// Gets or sets the parent column
        /// </summary>
        public string Column { get; set; }
        /// <summary>
        /// Gets or sets the referenced table
        /// </summary>
        public string ReferencedEntity { get; set; }
        /// <summary>
        /// Gets or sets the referenced column
        /// </summary>
        public string ReferencedColumn { get; set; }
        /// <summary>
        /// Gets or sets the delete action
        /// </summary>
        public OnAction OnDeleteAction { get; set; }
        /// <summary>
        /// Gets or sets the update action
        /// </summary>
        public OnAction OnUpdateAction { get; set; }
    }

    public enum OnAction
    {
        CASACADE = 0,
        NO_ACTION = 1,
        SET_NULL = 2,
        SET_DEFAULT = 3
    }
}