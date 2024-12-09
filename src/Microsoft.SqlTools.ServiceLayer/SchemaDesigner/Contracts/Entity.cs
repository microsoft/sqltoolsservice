//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class Entity
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public Column[] Columns { get; set; }
    }
}