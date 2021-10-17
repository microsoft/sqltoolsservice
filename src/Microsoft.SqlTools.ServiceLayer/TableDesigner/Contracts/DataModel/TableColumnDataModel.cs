//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The data model of a table column object
    /// </summary>
    public class TableColumnDataModel : ObjectDataModelBase
    {
        public TableColumnDataModel()
        {
            this.Type = new DropdownProperties();
            this.Length = new InputBoxProperties();
            this.AllowNulls = new CheckBoxProperties();
            this.DefaultValue = new InputBoxProperties();
            this.IsPrimaryKey = new CheckBoxProperties();
        }

        public DropdownProperties Type { get; set; }

        public InputBoxProperties Length { get; set; }

        public CheckBoxProperties AllowNulls { get; set; }

        public InputBoxProperties DefaultValue { get; set; }

        public CheckBoxProperties IsPrimaryKey { get; set; }
    }
}