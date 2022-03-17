//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum InputType
    {
        [EnumMember(Value = "text")]
        Text,
        [EnumMember(Value = "number")]
        Number
    }
    /// <summary>
    /// Inputbox properties
    /// </summary>
    public class InputBoxProperties : ComponentPropertiesBase
    {
        public string Value { get; set; }

        public InputType InputType { get; set; } = InputType.Text;
    }
}