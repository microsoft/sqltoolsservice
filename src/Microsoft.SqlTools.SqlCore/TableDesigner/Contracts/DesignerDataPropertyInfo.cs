//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// Represents a component/property in the table designer
    /// </summary>
    [DataContract]
    public class DesignerDataPropertyInfo
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        [DataMember(Name = "propertyName")]
        public string PropertyName { get; set; }

        /// <summary>
        /// The description of the property
        /// </summary>
        [DataMember(Name = "description")]
        public string Description { get; set; }

        /// <summary>
        /// The component type of the property
        /// </summary>
        [DataMember(Name = "componentType")]
        public DesignerComponentType ComponentType { get; set; }

        /// <summary>
        /// The name of the group the property will be placed in whe displayed in
        /// </summary>
        [DataMember(Name = "group")]
        public string Group { get; set; }

        /// <summary>
        /// The name of the group the property will be placed in whe displayed in
        /// </summary>
        [DataMember(Name = "showInPropertiesView")]
        public bool ShowInPropertiesView { get; set; } = true;


        /// <summary>
        /// The properties of component
        /// </summary>
        [DataMember(Name = "componentProperties")]
        public ComponentPropertiesBase ComponentProperties { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DesignerComponentType
    {
        [EnumMember(Value = "checkbox")]
        Checkbox,
        [EnumMember(Value = "dropdown")]
        Dropdown,
        [EnumMember(Value = "input")]
        Input,
        [EnumMember(Value = "table")]
        Table
    }
}