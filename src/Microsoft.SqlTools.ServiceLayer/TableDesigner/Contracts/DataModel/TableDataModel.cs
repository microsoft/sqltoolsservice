//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The data model for a table object
    /// </summary>
    public class TableDataModel : ObjectDataModelBase
    {
        public DropdownProperties Schema { get; set; } = new DropdownProperties();

        public InputBoxProperties Description { get; set; } = new InputBoxProperties();

        public TableColumnCollection Columns { get; set; } = new TableColumnCollection();

        public InputBoxProperties Script { get; set; } = new InputBoxProperties();
    }

    public class TableColumnCollection : TableProperties<TableColumnDataModel>
    {
        [JsonIgnore]
        protected override string NewObjectNamePrefix { get { return "column"; } }

        protected override TableColumnDataModel CreateNew(string name)
        {
            //TODO: Add the default values
            var column = new TableColumnDataModel();
            column.Name.Value = this.GetDefaultNewObjectName();
            return column;
        }
    }
}