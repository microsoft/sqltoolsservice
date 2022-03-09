//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The view model for a table object
    /// </summary>
    public class TableViewModel : ObjectViewModelBase
    {
        public DropdownProperties Schema { get; set; } = new DropdownProperties();

        public InputBoxProperties Description { get; set; } = new InputBoxProperties();

        public DropdownProperties GraphTableType { get; set; } = new DropdownProperties();

        public CheckBoxProperties IsMemoryOptimized { get; set; } = new CheckBoxProperties();

        public DropdownProperties Durability { get; set; } = new DropdownProperties();

        public CheckBoxProperties IsSystemVersioningEnabled { get; set; } = new CheckBoxProperties();

        public DropdownProperties ExistingHistoryTable { get; set; } = new DropdownProperties();

        public CheckBoxProperties AutoCreateHistoryTable { get; set; } = new CheckBoxProperties();

        public InputBoxProperties NewHistoryTableName { get; set; } = new InputBoxProperties();

        public InputBoxProperties PrimaryKeyName { get; set; } = new InputBoxProperties();

        public CheckBoxProperties PrimaryKeyIsClustered { get; set; } = new CheckBoxProperties();

        public TableComponentProperties<IndexedColumnSpecification> PrimaryKeyColumns { get; set; } = new TableComponentProperties<IndexedColumnSpecification>();

        public TableComponentProperties<TableColumnViewModel> Columns { get; set; } = new TableComponentProperties<TableColumnViewModel>();

        public TableComponentProperties<ForeignKeyViewModel> ForeignKeys { get; set; } = new TableComponentProperties<ForeignKeyViewModel>();

        public TableComponentProperties<CheckConstraintViewModel> CheckConstraints { get; set; } = new TableComponentProperties<CheckConstraintViewModel>();

        public TableComponentProperties<EdgeConstraintViewModel> EdgeConstraints { get; set; } = new TableComponentProperties<EdgeConstraintViewModel>();

        public TableComponentProperties<IndexViewModel> Indexes { get; set; } = new TableComponentProperties<IndexViewModel>();

        public InputBoxProperties Script { get; set; } = new InputBoxProperties();
    }
}