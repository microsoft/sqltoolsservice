//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Utilities
{
    internal interface IUnion
    {
        object Value { get; set; }
    }

    [JsonConverter(typeof(UnionJsonSerializer))]
    public class Union<T1, T2> : IUnion
    {
        protected T1 value1;
        protected T2 value2;

        internal Union() {}
        
        public object Value
        {
            get => (object) value1 ?? value2;
            set
            {
                if (value == null)
                {
                    throw new ArgumentException();
                }
                
                if (value is T1)
                {
                    value1 = (T1) value;
                } 
                else if (value is T2)
                {
                    value2 = (T2) value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }

    public class UnionJsonSerializer : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((IUnion)value).Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Check for null first
            JToken jToken = JToken.Load(reader);
            if (jToken.Type == JTokenType.Null)
            {
                return null;
            }
            
            // Cast to an object
            JObject jObject = (JObject)jToken;

            // Try to convert to 
            object value = null;
            foreach (Type t in objectType.GenericTypeArguments)
            {
                try
                {
                    value = jObject.ToObject(t);
                    break;
                }
                catch (Exception)
                {
                    // Ignore any failure to cast, we'll throw if we exhaust all the conversion options
                }
            }

            if (value == null)
            {
                throw new InvalidCastException();
            }
            
            IUnion result = (IUnion)Activator.CreateInstance(objectType);
            result.Value = value;
            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(IUnion));
        }
    }
}