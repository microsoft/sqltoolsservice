//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public abstract class TableProperties<T> : ComponentPropertiesBase
    {
        public string[] Columns { get; set; }

        public string ObjectTypeDisplayName { get; set; }

        public DesignerDataPropertyInfo[] ItemProperties { get; set; }

        public T[] Data { get; set; }
    }
}