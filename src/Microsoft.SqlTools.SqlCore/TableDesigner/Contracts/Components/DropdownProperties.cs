//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// Dropdown properties
    /// </summary>
    [DataContract]
    public class DropdownProperties : ComponentPropertiesBase
    {
        [DataMember(Name = "value")]
        public string Value { get; set; }
        [DataMember(Name = "values")]
        public List<string> Values { get; set; }
    }
}