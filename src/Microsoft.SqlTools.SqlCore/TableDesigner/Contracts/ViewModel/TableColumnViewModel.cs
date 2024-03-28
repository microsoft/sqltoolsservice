//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of a table column object
    /// </summary>
    [DataContract]
    public class TableColumnViewModel : ObjectViewModelBase
    {
        [DataMember(Name = "advancedType")]
        public DropdownProperties AdvancedType { get; set; } = new DropdownProperties();
        [DataMember(Name = "type")]
        public DropdownProperties Type { get; set; } = new DropdownProperties();
        [DataMember(Name = "length")]
        public InputBoxProperties Length { get; set; } = new InputBoxProperties();
        [DataMember(Name = "scale")]
        public InputBoxProperties Scale { get; set; } = new InputBoxProperties();
        [DataMember(Name = "precision")]
        public InputBoxProperties Precision { get; set; } = new InputBoxProperties();
        [DataMember(Name = "allowNulls")]
        public CheckBoxProperties AllowNulls { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "defaultValue")]
        public InputBoxProperties DefaultValue { get; set; } = new InputBoxProperties();
        [DataMember(Name = "isPrimaryKey")]
        public CheckBoxProperties IsPrimaryKey { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "isIdentity")]
        public CheckBoxProperties IsIdentity { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "identitySeed")]
        public InputBoxProperties IdentitySeed { get; set; } = new InputBoxProperties();
        [DataMember(Name = "identityIncrement")]
        public InputBoxProperties IdentityIncrement { get; set; } = new InputBoxProperties();
        [DataMember(Name = "isComputed")]
        public CheckBoxProperties IsComputed { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "computedFormula")]
        public InputBoxProperties ComputedFormula { get; set; } = new InputBoxProperties();
        [DataMember(Name = "isComputedPersisted")]
        public CheckBoxProperties IsComputedPersisted { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "isComputedPersistedNullable")]
        public CheckBoxProperties IsComputedPersistedNullable { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "generatedAlwaysAs")]
        public DropdownProperties GeneratedAlwaysAs { get; set; } = new DropdownProperties();
        [DataMember(Name = "isHidden")]
        public CheckBoxProperties IsHidden { get; set; } = new CheckBoxProperties();
        [DataMember(Name = "defaultConstraintName")]
        public InputBoxProperties DefaultConstraintName { get; set; } = new InputBoxProperties();
        [DataMember(Name = "canBeDeleted")]
        public bool CanBeDeleted { get; set; } = true;
    }
}