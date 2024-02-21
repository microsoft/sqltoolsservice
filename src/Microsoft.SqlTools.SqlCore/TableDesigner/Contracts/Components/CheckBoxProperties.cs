//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// Checkbox properties
    /// </summary>
    [DataContract]
    public class CheckBoxProperties : ComponentPropertiesBase
    {
        [DataMember(Name = "checked")]
        public bool Checked { get; set; }
    }
}