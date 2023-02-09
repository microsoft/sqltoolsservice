//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Represents a component/property with tab info in the table designer
    /// </summary>
    public class DesignerDataPropertyWithTabInfo : DesignerDataPropertyInfo
    {
        /// <summary>
        /// The tab of the property
        /// </summary>
        public string Tab { get; set; }
    }
}