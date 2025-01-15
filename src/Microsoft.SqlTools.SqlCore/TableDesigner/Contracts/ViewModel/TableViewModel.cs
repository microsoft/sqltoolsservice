//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The view model for a table object
    /// </summary>
    
    [DataContract]
    public class TableViewModel : ObjectViewModelBase
    {
        [DataMember(Name = "schema")]
        public DropdownProperties Schema { get; set; } = new DropdownProperties();
        [DataMember(Name = "graphTableType")]
        public DropdownProperties GraphTableType { get; set; } = new DropdownProperties();
        [DataMember(Name = "isMemoryOptimized")]
        public CheckBoxProperties IsMemoryOptimized { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "durability")]
        public DropdownProperties Durability { get; set; } = new DropdownProperties();
        [DataMember(Name = "isSystemVersioningEnabled")]
        public CheckBoxProperties IsSystemVersioningEnabled { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "existingHistoryTable")]
        public DropdownProperties ExistingHistoryTable { get; set; } = new DropdownProperties();
        [DataMember(Name = "autoCreateHistoryTable")]
        public CheckBoxProperties AutoCreateHistoryTable { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "newHistoryTableName")]
        public InputBoxProperties NewHistoryTableName { get; set; } = new InputBoxProperties();
        [DataMember(Name = "primaryKeyName")]
        public InputBoxProperties PrimaryKeyName { get; set; } = new InputBoxProperties();
        [DataMember(Name = "primaryKeyDescription")]
        public InputBoxProperties PrimaryKeyDescription { get; set; } = new InputBoxProperties();
        [DataMember(Name = "primaryKeyIsClustered")]
        public CheckBoxProperties PrimaryKeyIsClustered { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "primaryKeyColumns")]
        public TableComponentProperties<IndexedColumnSpecification> PrimaryKeyColumns { get; set; } = new TableComponentProperties<IndexedColumnSpecification>();
        [DataMember(Name = "columns")]
        public TableComponentProperties<TableColumnViewModel> Columns { get; set; } = new TableComponentProperties<TableColumnViewModel>();
        [DataMember(Name = "foreignKeys")]
        public TableComponentProperties<ForeignKeyViewModel> ForeignKeys { get; set; } = new TableComponentProperties<ForeignKeyViewModel>();
        [DataMember(Name = "checkConstraints")]
        public TableComponentProperties<CheckConstraintViewModel> CheckConstraints { get; set; } = new TableComponentProperties<CheckConstraintViewModel>();
        [DataMember(Name = "edgeConstraints")]
        public TableComponentProperties<EdgeConstraintViewModel> EdgeConstraints { get; set; } = new TableComponentProperties<EdgeConstraintViewModel>();
        [DataMember(Name = "indexes")]
        public TableComponentProperties<IndexViewModel> Indexes { get; set; } = new TableComponentProperties<IndexViewModel>();
        [DataMember(Name = "columnStoreIndexes")]
        public TableComponentProperties<ColumnStoreIndexViewModel> ColumnStoreIndexes { get; set; } = new TableComponentProperties<ColumnStoreIndexViewModel>();
        [DataMember(Name = "script")]
        public InputBoxProperties Script { get; set; } = new InputBoxProperties();
    }
}