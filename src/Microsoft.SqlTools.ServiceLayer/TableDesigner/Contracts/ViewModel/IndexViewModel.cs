//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of index.
    /// </summary>
    public class IndexViewModel : ObjectViewModelBase
    {
        public CheckBoxProperties Enabled { get; set; } = new CheckBoxProperties();

        public CheckBoxProperties IsClustered { get; set; } = new CheckBoxProperties();

        public CheckBoxProperties IsUnique { get; set; } = new CheckBoxProperties();
        public CheckBoxProperties IsHash { get; set; } = new CheckBoxProperties();

        public InputBoxProperties BucketCount { get; set; } = new InputBoxProperties();
        public InputBoxProperties ColumnsDisplayValue { get; set; } = new InputBoxProperties();
        public InputBoxProperties FilterPredicate { get; set; } = new InputBoxProperties();

        public TableComponentProperties<IndexedColumnSpecification> Columns { get; set; } = new TableComponentProperties<IndexedColumnSpecification>();
        public TableComponentProperties<IndexIncludedColumnSpecification> IncludedColumns { get; set; } = new TableComponentProperties<IndexIncludedColumnSpecification>();
    }

    public class IndexedColumnSpecification
    {
        public DropdownProperties Column { get; set; } = new DropdownProperties();

        public CheckBoxProperties Ascending { get; set; } = new CheckBoxProperties();
    }

    public class IndexIncludedColumnSpecification
    {
        public DropdownProperties Column { get; set; } = new DropdownProperties();
    }
}