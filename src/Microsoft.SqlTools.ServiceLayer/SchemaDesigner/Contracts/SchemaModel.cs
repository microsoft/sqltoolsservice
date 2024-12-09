//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaModel
    {
        /// <summary>
        /// Gets or sets the entities in the schema
        /// </summary>
        public Entity[] Entities { get; set; }
        /// <summary>
        /// Gets or sets the relationships in the schema
        /// </summary>
        public Relationship[] Relationships { get; set; }
    }
}