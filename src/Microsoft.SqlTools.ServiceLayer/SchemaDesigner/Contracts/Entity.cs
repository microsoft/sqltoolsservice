//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class Entity
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public List<Column> Columns { get; set; }
    }
}