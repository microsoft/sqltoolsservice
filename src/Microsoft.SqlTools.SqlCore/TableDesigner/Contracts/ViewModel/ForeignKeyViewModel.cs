//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of foreign key.
    /// </summary>
    [DataContract]
    public class ForeignKeyViewModel : ObjectViewModelBase
    {
        [DataMember(Name = "enabled")]
        public CheckBoxProperties Enabled { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "onDeleteAction")]
        public DropdownProperties OnDeleteAction { get; set; } = new DropdownProperties();
        [DataMember(Name = "onUpdateAction")]
        public DropdownProperties OnUpdateAction { get; set; } = new DropdownProperties();
        [DataMember(Name = "foreignTable")]
        public DropdownProperties ForeignTable { get; set; } = new DropdownProperties();
        [DataMember(Name = "isNotForReplication")]
        public CheckBoxProperties IsNotForReplication { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "columns")]
        public TableComponentProperties<ForeignKeyColumnMapping> Columns { get; set; } = new TableComponentProperties<ForeignKeyColumnMapping>();
    }

    [DataContract]
    public class ForeignKeyColumnMapping
    {
        [DataMember(Name = "column")]
        public DropdownProperties Column { get; set; } = new DropdownProperties();
        [DataMember(Name = "foreignColumn")]
        public DropdownProperties ForeignColumn { get; set; } = new DropdownProperties();
    }
}