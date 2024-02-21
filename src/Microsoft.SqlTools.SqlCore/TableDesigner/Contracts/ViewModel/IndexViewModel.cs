//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of index.
    /// </summary>
    [DataContract]
    public class IndexViewModel : ObjectViewModelBase
    {
        [DataMember(Name = "enabled")]
        public CheckBoxProperties Enabled { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "isClustered")]
        public CheckBoxProperties IsClustered { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "isUnique")]
        public CheckBoxProperties IsUnique { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "isHash")]
        public CheckBoxProperties IsHash { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "bucketCount")]
        public InputBoxProperties BucketCount { get; set; } = new InputBoxProperties();
        [DataMember(Name = "columnsDisplayValue")]
        public InputBoxProperties ColumnsDisplayValue { get; set; } = new InputBoxProperties();
        [DataMember(Name = "filterPredicate")]
        public InputBoxProperties FilterPredicate { get; set; } = new InputBoxProperties();
        [DataMember(Name = "columns")]
        public TableComponentProperties<IndexedColumnSpecification> Columns { get; set; } = new TableComponentProperties<IndexedColumnSpecification>();
        [DataMember(Name = "includedColumns")]
        public TableComponentProperties<IndexIncludedColumnSpecification> IncludedColumns { get; set; } = new TableComponentProperties<IndexIncludedColumnSpecification>();
    }

    [DataContract]
    public class IndexedColumnSpecification
    {
        [DataMember(Name = "column")]
        public DropdownProperties Column { get; set; } = new DropdownProperties();
        [DataMember(Name = "ascending")]
        public CheckBoxProperties Ascending { get; set; } = new CheckBoxProperties();
    }

    [DataContract]
    public class IndexIncludedColumnSpecification
    {
        [DataMember(Name = "column")]
        public DropdownProperties Column { get; set; } = new DropdownProperties();
    }
}