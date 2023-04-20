//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Models
{
    public class MaterializedViewSchemaInfo
    {
        public string Name { get; set; }

        public string Folder { get; set; }

        public IEnumerable<MaterializedViewColumnInfo> OrderedColumns { get; set; }
    }

    public class MaterializedViewColumnInfo
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string CslType { get; set; }
    }
}
