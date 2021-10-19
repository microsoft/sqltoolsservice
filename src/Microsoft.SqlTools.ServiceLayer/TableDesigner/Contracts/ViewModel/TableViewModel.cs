//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The view model for a table object
    /// </summary>
    public class TableViewModel : ObjectViewModelBase
    {
        public DropdownProperties Schema { get; set; } = new DropdownProperties();

        public InputBoxProperties Description { get; set; } = new InputBoxProperties();

        public TableColumnCollection Columns { get; set; } = new TableColumnCollection();

        public InputBoxProperties Script { get; set; } = new InputBoxProperties();
    }

    public class TableColumnCollection : TableComponentProperties<TableColumnViewModel>
    {
        [JsonIgnore]
        protected override string NewObjectNamePrefix { get { return "column"; } }

        protected override TableColumnViewModel CreateNew(string name)
        {
            //TODO: Add the default values
            var column = new TableColumnViewModel();
            column.Name.Value = this.GetDefaultNewObjectName();
            return column;
        }
    }
}