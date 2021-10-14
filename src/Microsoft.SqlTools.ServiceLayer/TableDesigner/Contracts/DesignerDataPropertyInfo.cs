//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Represents a component/property in the table designer
    /// </summary>
    public class DesignerDataPropertyInfo
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        public string PropertyName { get; set; }

        public string ComponentType { get; set; }

        /// <summary>
        /// The name of the group the property will be placed in whe displayed in
        /// </summary>
        public string Group { get; set; }


        /// <summary>
        /// The properties of component
        /// </summary>
        public ComponentPropertiesBase ComponentProperties { get; set; }
    }
}