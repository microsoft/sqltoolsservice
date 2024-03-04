//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of columnstore index.
    /// </summary>
    [DataContract]
    public class ColumnStoreIndexViewModel : ObjectViewModelBase
    {
        [DataMember(Name = "isClustered")]
        public CheckBoxProperties IsClustered { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "columnsDisplayValue")]
        public InputBoxProperties ColumnsDisplayValue { get; set; } = new InputBoxProperties();
        [DataMember(Name = "filterPredicate")]
        public InputBoxProperties FilterPredicate { get; set; } = new InputBoxProperties();
        [DataMember(Name = "columns")]
        public TableComponentProperties<ColumnStoreIndexedColumnSpecification> Columns { get; set; } = new TableComponentProperties<ColumnStoreIndexedColumnSpecification>();
    }

    [DataContract]
    public class ColumnStoreIndexedColumnSpecification
    {
        [DataMember(Name = "column")]
        public DropdownProperties Column { get; set; } = new DropdownProperties();
        [DataMember(Name = "ascending")]
        public CheckBoxProperties Ascending { get; set; } = new CheckBoxProperties();
    }
}