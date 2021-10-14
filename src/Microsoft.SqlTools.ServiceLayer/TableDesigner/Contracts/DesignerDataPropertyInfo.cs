//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Represents a component in the table designer
    /// </summary>
    public class DesignerDataPropertyInfo
    {
        public string PropertyName { get; set; }

        public string ComponentType { get; set; }

        public string Group { get; set; }

        public ComponentPropertiesBase ComponentProperties { get; set; }
    }
}