//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// Table designer's view definition, there are predefined common properties.
    /// Specify the additional properties in this class.
    /// </summary>
    [DataContract]
    public class TableDesignerView
    {
        [DataMember(Name = "additionalTableProperties")]
        public List<DesignerDataPropertyInfo> AdditionalTableProperties { get; set; } = new List<DesignerDataPropertyInfo>();
        [DataMember(Name = "columnTableOptions")]
        public BuiltinTableOptions ColumnTableOptions { get; set; } = new BuiltinTableOptions();
        [DataMember(Name = "foreignKeyTableOptions")]
        public BuiltinTableOptions ForeignKeyTableOptions { get; set; } = new BuiltinTableOptions();
        [DataMember(Name = "foreignKeyColumnMappingTableOptions")]
        public BuiltinTableOptions ForeignKeyColumnMappingTableOptions { get; set; } = new BuiltinTableOptions();
        [DataMember(Name = "checkConstraintTableOptions")]
        public BuiltinTableOptions CheckConstraintTableOptions { get; set; } = new BuiltinTableOptions();
        [DataMember(Name = "indexTableOptions")]
        public BuiltinTableOptions IndexTableOptions { get; set; } = new BuiltinTableOptions();
        [DataMember(Name = "indexColumnSpecificationTableOptions")]
        public BuiltinTableOptions IndexColumnSpecificationTableOptions { get; set; } = new BuiltinTableOptions();
        [DataMember(Name = "additionalPrimaryKeyProperties")]
        public List<DesignerDataPropertyInfo> AdditionalPrimaryKeyProperties { get; set; } = new List<DesignerDataPropertyInfo>();
        [DataMember(Name = "additionalComponents")]
        public List<DesignerDataPropertyWithTabInfo> AdditionalComponents { get; set; } = new List<DesignerDataPropertyWithTabInfo>();
        [DataMember(Name = "primaryKeyColumnSpecificationTableOptions")]
        public BuiltinTableOptions PrimaryKeyColumnSpecificationTableOptions = new BuiltinTableOptions();
        [DataMember(Name = "additionalTabs")]
        public List<DesignerTabView> AdditionalTabs { get; } = new List<DesignerTabView>();
        [DataMember(Name = "useAdvancedSaveMode")]
        public bool UseAdvancedSaveMode { get; set; }
    }

    [DataContract]
    public class BuiltinTableOptions
    {
        [DataMember(Name = "showTable")]
        public bool ShowTable { get; set; } = true;
        [DataMember(Name = "propertiesToDisplay")]
        public List<string> PropertiesToDisplay { get; set; } = new List<string>();
        [DataMember(Name = "canAddRows")]
        public bool CanAddRows { get; set; } = true;
        [DataMember(Name = "canRemoveRows")]
        public bool CanRemoveRows { get; set; } = true;
        [DataMember(Name = "canMoveRows")]
        public bool CanMoveRows { get; set; } = false;
        [DataMember(Name = "canInsertRows")]
        public bool CanInsertRows { get; set; } = false;
        [DataMember(Name = "additionalProperties")]
        public List<DesignerDataPropertyInfo> AdditionalProperties { get; set; } = new List<DesignerDataPropertyInfo>();
        [DataMember(Name = "removeRowConfirmationMessage")]
        public string RemoveRowConfirmationMessage { get; set; }
        [DataMember(Name = "showRemoveRowConfirmation")]
        public bool ShowRemoveRowConfirmation { get; set; } = false;
    }

    [DataContract]
    public class DesignerTabView
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "properties")]
        public List<DesignerDataPropertyInfo> Components { get; } = new List<DesignerDataPropertyInfo>();
    }
}