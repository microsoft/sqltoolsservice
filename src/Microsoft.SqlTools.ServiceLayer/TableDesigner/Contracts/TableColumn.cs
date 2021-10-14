//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class TableColumn
    {
        public InputBoxProperties Name { get; set; }

        public DropdownProperties Type { get; set; }

        public InputBoxProperties Length { get; set; }

        public CheckBoxProperties AllowNulls { get; set; }

        public InputBoxProperties DefaultValue { get; set; }

        public CheckBoxProperties IsPrimaryKey { get; set; }
    }
}