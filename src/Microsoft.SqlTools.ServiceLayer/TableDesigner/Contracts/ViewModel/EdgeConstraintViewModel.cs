//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of edge constraint.
    /// </summary>
    public class EdgeConstraintViewModel : ObjectViewModelBase
    {
        public CheckBoxProperties Enabled { get; set; } = new CheckBoxProperties();

        public DropdownProperties OnDeleteAction { get; set; } = new DropdownProperties();

        public TableComponentProperties<EdgeConstraintClause> Clauses { get; set; } = new TableComponentProperties<EdgeConstraintClause>();

        public InputBoxProperties ClausesDisplayValue { get; set; } = new InputBoxProperties();
    }

    public class EdgeConstraintClause
    {
        public DropdownProperties FromTable { get; set; } = new DropdownProperties();

        public DropdownProperties ToTable { get; set; } = new DropdownProperties();
    }
}