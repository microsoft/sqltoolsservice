//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The data model for a table object
    /// </summary>
    public class TableDataModel
    {
        public InputBoxProperties Name { get; set; }

        public DropdownProperties Schema { get; set; }

        public InputBoxProperties Description { get; set; }

        public TableColumnCollection Columns { get; set; }

        public InputBoxProperties Script { get; set; }
    }

    public class TableColumnCollection : TableProperties<TableColumn>
    {
    }
}