//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of columnstore index.
    /// </summary>
    public class ColumnStoreIndexViewModel : ObjectViewModelBase
    {

        public CheckBoxProperties IsClustered { get; set; } = new CheckBoxProperties();

        public InputBoxProperties ColumnsDisplayValue { get; set; } = new InputBoxProperties();
        public InputBoxProperties FilterPredicate { get; set; } = new InputBoxProperties();

        public TableComponentProperties<ColumnStoreIndexedColumnSpecification> Columns { get; set; } = new TableComponentProperties<ColumnStoreIndexedColumnSpecification>();
    }

    public class ColumnStoreIndexedColumnSpecification
    {
        public DropdownProperties Column { get; set; } = new DropdownProperties();

        public CheckBoxProperties Ascending { get; set; } = new CheckBoxProperties();
    }
}