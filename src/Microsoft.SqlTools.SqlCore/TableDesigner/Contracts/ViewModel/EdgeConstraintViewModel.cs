//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of edge constraint.
    /// </summary>
    [DataContract]
    public class EdgeConstraintViewModel : ObjectViewModelBase
    {
        [DataMember(Name = "enabled")]
        public CheckBoxProperties Enabled { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "onDeleteAction")]
        public DropdownProperties OnDeleteAction { get; set; } = new DropdownProperties();
        [DataMember(Name = "clauses")]
        public TableComponentProperties<EdgeConstraintClause> Clauses { get; set; } = new TableComponentProperties<EdgeConstraintClause>();
        [DataMember(Name = "clausesDisplayValue")]
        public InputBoxProperties ClausesDisplayValue { get; set; } = new InputBoxProperties();
    }

    [DataContract]
    public class EdgeConstraintClause
    {
        [DataMember(Name = "fromTable")]
        public DropdownProperties FromTable { get; set; } = new DropdownProperties();
        [DataMember(Name = "toTable")]
        public DropdownProperties ToTable { get; set; } = new DropdownProperties();
    }
}