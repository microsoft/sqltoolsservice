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

namespace Microsoft.SqlTools.DataProtocol.Contracts.Utilities
{
    internal class FlagsIntConverter : JsonConverter
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
            // TODO: Fix to handle nullables properly
            
            int[] values = JArray.Load(reader).Values<int>().ToArray();

            FieldInfo[] enumFields = objectType.GetFields(BindingFlags.Public | BindingFlags.Static);
            int setFlags = 0;
            foreach (FieldInfo enumField in enumFields)
            {
                int enumValue = (int) enumField.GetValue(null);
                
                // If there is a serialize value set for the enum value, look for that instead of the int value
                SerializeValueAttribute serializeValue = enumField.GetCustomAttribute<SerializeValueAttribute>();
                int searchValue = serializeValue?.Value ?? enumValue;
                if (Array.IndexOf(values, searchValue) >= 0)
                {
                    setFlags |= enumValue;
                }
            }

            return Enum.ToObject(objectType, setFlags);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            FieldInfo[] enumFields = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static);
            List<int> setFlags = new List<int>();
            foreach (FieldInfo enumField in enumFields)
            {
                // Make sure the flag is set before doing expensive reflection
                int enumValue = (int)enumField.GetValue(null);
                if (((int) value & enumValue) == 0)
                {
                    continue;
                }
                
                // If there is a serialize value set for the member, use that instead of the int value
                SerializeValueAttribute serializeValue = enumField.GetCustomAttribute<SerializeValueAttribute>();
                int flagValue = serializeValue?.Value ?? enumValue; 
                setFlags.Add(flagValue);
            }

            string joinedFlags = string.Join(", ", setFlags);
            writer.WriteRawValue($"[{joinedFlags}]");
        }
        
        #endregion

        [AttributeUsage(AttributeTargets.Field)]
        internal class SerializeValueAttribute : Attribute
        {
            public int Value { get; }

            public SerializeValueAttribute(int value)
            {
                Value = value;
            }
        }  
    }
}