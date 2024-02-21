//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DesignerEditType
    {
        [EnumMember(Value = "Add")]
        Add,
        [EnumMember(Value = "Remove")]
        Remove,
        [EnumMember(Value = "Update")]
        Update,
        [EnumMember(Value = "Move")]
        Move,
    }

    /// <summary>
    /// The information about a change made inside the table designer.
    /// </summary>
    [DataContract]
    public class TableDesignerChangeInfo
    {
        [DataMember(Name = "type")]
        public DesignerEditType Type { get; set; }
        [DataMember(Name = "path")]
        public object[] Path { get; set; }
        [DataMember(Name = "value")]
        public object Value { get; set; }
    }
}