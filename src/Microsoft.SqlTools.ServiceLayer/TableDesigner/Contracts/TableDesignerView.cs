//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Table designer's view definition, there are predefined common properties.
    /// Specify the additional properties in this class.
    /// </summary>
    public class TableDesignerView
    {
        public List<DesignerDataPropertyInfo> AdditionalTableProperties { get; set; } = new List<DesignerDataPropertyInfo>();
        public BuiltinTableOptions ColumnTableOptions { get; set; } = new BuiltinTableOptions();
        public BuiltinTableOptions ForeignKeyTableOptions { get; set; } = new BuiltinTableOptions();
        public BuiltinTableOptions ForeignKeyColumnMappingTableOptions { get; set; } = new BuiltinTableOptions();
        public BuiltinTableOptions CheckConstraintTableOptions { get; set; } = new BuiltinTableOptions();
        public BuiltinTableOptions IndexTableOptions { get; set; } = new BuiltinTableOptions();
        public BuiltinTableOptions IndexColumnSpecificationTableOptions { get; set; } = new BuiltinTableOptions();
        public List<DesignerDataPropertyInfo> AdditionalPrimaryKeyProperties { get; set; } = new List<DesignerDataPropertyInfo>();
        public BuiltinTableOptions PrimaryKeyColumnSpecificationTableOptions = new BuiltinTableOptions();
        public List<DesignerTabView> AdditionalTabs { get; } = new List<DesignerTabView>();
        public bool UseAdvancedSaveMode { get; set; }
    }

    public class BuiltinTableOptions
    {
        public bool ShowTable { get; set; } = true;
        public List<string> PropertiesToDisplay { get; set; } = new List<string>();
        public bool CanAddRows { get; set; } = true;
        public bool CanRemoveRows { get; set; } = true;
        public bool CanMoveRows { get; set; } = false;
        public bool CanInsertRows { get; set; } = false;
        public List<DesignerDataPropertyInfo> AdditionalProperties { get; set; } = new List<DesignerDataPropertyInfo>();
        public string RemoveRowConfirmationMessage { get; set; }
        public bool ShowRemoveRowConfirmation { get; set; } = false;
    }

    public class DesignerTabView
    {
        public string Title { get; set; }
        public List<DesignerDataPropertyInfo> Components { get; } = new List<DesignerDataPropertyInfo>();
    }
}