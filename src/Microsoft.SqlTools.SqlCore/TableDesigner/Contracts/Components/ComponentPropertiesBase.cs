//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// Base class for component properties
    /// </summary>
    [DataContract]
    [KnownType(typeof(CheckBoxProperties))]
    [KnownType(typeof(DropdownProperties))]
    [KnownType(typeof(InputBoxProperties))]
    public abstract class ComponentPropertiesBase
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "ariaLabel")]
        public string AriaLabel { get; set; }
        [DataMember(Name = "width")]
        public Nullable<int> Width { get; set; }
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = true;
    }
}