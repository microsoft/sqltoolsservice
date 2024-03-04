//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// Represents a component/property with tab info in the table designer
    /// </summary>
    [DataContract]
    public class DesignerDataPropertyWithTabInfo : DesignerDataPropertyInfo
    {
        /// <summary>
        /// The tab of the property
        /// </summary>
        [DataMember(Name = "tab")]
        public string Tab { get; set; }
    }
}