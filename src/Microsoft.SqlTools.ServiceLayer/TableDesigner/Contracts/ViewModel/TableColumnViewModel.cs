//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of a table column object
    /// </summary>
    public class TableColumnViewModel : ObjectViewModelBase
    {
        public DropdownProperties Type { get; set; } = new DropdownProperties();

        public InputBoxProperties Length { get; set; } = new InputBoxProperties();

        public InputBoxProperties Scale { get; set; } = new InputBoxProperties();

        public InputBoxProperties Precision { get; set; } = new InputBoxProperties();

        public CheckBoxProperties AllowNulls { get; set; } = new CheckBoxProperties();

        public InputBoxProperties DefaultValue { get; set; } = new InputBoxProperties();

        public CheckBoxProperties IsPrimaryKey { get; set; } = new CheckBoxProperties();

        public CheckBoxProperties IsIdentity { get; set; } = new CheckBoxProperties();

        public InputBoxProperties IdentitySeed { get; set; } = new InputBoxProperties();

        public InputBoxProperties IdentityIncrement { get; set; } = new InputBoxProperties();

        public DropdownProperties GeneratedAlwaysAs { get; set; } = new DropdownProperties();

        public CheckBoxProperties IsHidden { get; set; } = new CheckBoxProperties();

        public InputBoxProperties DefaultConstraintName { get; set; } = new InputBoxProperties();

        public bool CanBeDeleted { get; set; } = true;
    }
}