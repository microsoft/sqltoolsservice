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
    public enum DesignerEditType
    {
        [EnumMember(Value = "Add")]
        Add,
        [EnumMember(Value = "Remove")]
        Remove,
        [EnumMember(Value = "Update")]
        Update,
    }

    public class TableDesignerChangeInfo
    {
        public DesignerEditType Type { get; set; }

        public object Property { get; set; }

        public object Value { get; set; }
    }
}