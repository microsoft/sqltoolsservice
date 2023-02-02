//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of foreign key.
    /// </summary>
    public class ForeignKeyViewModel : ObjectViewModelBase
    {
        public CheckBoxProperties Enabled { get; set; } = new CheckBoxProperties();

        public DropdownProperties OnDeleteAction { get; set; } = new DropdownProperties();

        public DropdownProperties OnUpdateAction { get; set; } = new DropdownProperties();

        public DropdownProperties ForeignTable { get; set; } = new DropdownProperties();

        public CheckBoxProperties IsNotForReplication { get; set; } = new CheckBoxProperties();

        public TableComponentProperties<ForeignKeyColumnMapping> Columns { get; set; } = new TableComponentProperties<ForeignKeyColumnMapping>();
    }

    public class ForeignKeyColumnMapping
    {
        public DropdownProperties Column { get; set; } = new DropdownProperties();

        public DropdownProperties ForeignColumn { get; set; } = new DropdownProperties();
    }
}