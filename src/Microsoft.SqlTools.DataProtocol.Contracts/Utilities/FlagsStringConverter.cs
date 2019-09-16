//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Utilities
{
    internal class FlagsStringConverter : JsonConverter
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        #region Public Methods

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnum && objectType.GetCustomAttribute(typeof(FlagsAttribute)) != null;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jToken = JToken.Load(reader);
            if (jToken.Type == JTokenType.Null)
            {
                return null;
            }

            string[] values = ((JArray)jToken).Values<string>().ToArray();
            var pureType = NullableUtils.GetUnderlyingTypeIfNullable(objectType);

            FieldInfo[] enumFields = pureType.GetFields(BindingFlags.Public | BindingFlags.Static);
            int setFlags = 0;
            foreach (FieldInfo enumField in enumFields)
            {
                // If there is a serialize value set for the enum value, look for the instead of the int value
                SerializeValueAttribute serializeValue = enumField.GetCustomAttribute<SerializeValueAttribute>();
                string searchValue = serializeValue?.Value ?? enumField.Name;
                if (serializer.ContractResolver is CamelCasePropertyNamesContractResolver)
                {
                    searchValue = char.ToLowerInvariant(searchValue[0]) + searchValue.Substring(1);
                }

                // If the value is in the json array, or the int value into the flags
                if (Array.IndexOf(values, searchValue) >= 0)
                {
                    setFlags |= (int)enumField.GetValue(null);
                }
            }

            return Enum.ToObject(pureType, setFlags);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            FieldInfo[] enumFields = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static);
            List<string> setFlags = new List<string>();
            foreach (FieldInfo enumField in enumFields)
            {
                // Make sure the flag is set before doing any other work
                int enumValue = (int)enumField.GetValue(null);
                if (((int)value & enumValue) == 0)
                {
                    continue;
                }

                // If there is a serialize value set for the member, use that instead of the int value
                SerializeValueAttribute serializeValue = enumField.GetCustomAttribute<SerializeValueAttribute>();
                string flagValue = serializeValue?.Value ?? enumField.Name;
                if (serializer.ContractResolver is CamelCasePropertyNamesContractResolver)
                {
                    flagValue = char.ToLowerInvariant(flagValue[0]) + flagValue.Substring(1);
                }
                setFlags.Add($"\"{flagValue}\"");
            }

            string joinedFlags = string.Join(", ", setFlags);
            writer.WriteRawValue($"[{joinedFlags}]");
        }

        #endregion Public Methods

        [AttributeUsage(AttributeTargets.Field)]
        internal class SerializeValueAttribute : Attribute
        {
            public string Value { get; }

            public SerializeValueAttribute(string value)
            {
                Value = value;
            }
        }
    }
}