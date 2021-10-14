//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Table component properties
    /// </summary>
    public class TableProperties<T> : ComponentPropertiesBase
    {
        /// <summary>
        /// The column names to be displayed
        /// </summary>
        public string[] Columns { get; set; }

        /// <summary>
        /// The object type display name of the objects in this table
        /// </summary>
        public string ObjectTypeDisplayName { get; set; }

        /// <summary>
        /// All properties of the object.
        /// </summary>
        public DesignerDataPropertyInfo[] ItemProperties { get; set; }

        /// <summary>
        /// The object list.
        /// </summary>
        public T[] Data { get; set; }
    }
}