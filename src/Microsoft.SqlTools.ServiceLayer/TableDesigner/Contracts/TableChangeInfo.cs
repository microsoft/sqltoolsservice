//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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

    /// <summary>
    /// The information about a change made inside the table designer.
    /// </summary>
    public class TableDesignerChangeInfo
    {
        public DesignerEditType Type { get; set; }

        [JsonConverter(typeof(TableDesignerPropertyConverter))]
        public object Property { get; set; }

        public object Value { get; set; }
    }

    /// <summary>
    /// The property "Property" of <c>TableDesignerChangeInfo</c> could be string or <c>TableDesignerPropertyIdentifier</c>, use this custom converter to set the property value.
    /// </summary>
    public class TableDesignerPropertyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object property;
            if (reader.TokenType == JsonToken.StartObject)
            {
                TableDesignerPropertyIdentifier obj = serializer.Deserialize(reader, typeof(TableDesignerPropertyIdentifier)) as TableDesignerPropertyIdentifier;
                property = obj;
            }
            else
            {
                property = reader.Value;
            }
            return property;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // We don't need to serialize this class.
            throw new NotImplementedException();
        }
    }
}