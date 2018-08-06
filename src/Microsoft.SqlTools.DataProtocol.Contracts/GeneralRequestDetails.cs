//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.SqlTools.DataProtocol.Contracts
{
    public class GeneralRequestDetails
    {
        public GeneralRequestDetails()
        {
            Options = new Dictionary<string, object>();
        }

        public T GetOptionValue<T>(string name)
        {
            T result = default(T);
            if (Options != null && Options.ContainsKey(name))
            {
                object value = Options[name];
                try
                {
                    result = GetValueAs<T>(value);
                }
                catch
                {
                    result = default(T);
                    // TODO move logger to a utilities project
                    // Logger.Instance.Write(LogLevel.Warning, string.Format(CultureInfo.InvariantCulture,
                    //     "Cannot convert option value {0}:{1} to {2}", name, value ?? "", typeof(T)));
                }
            }
            return result;
        }

        public static T GetValueAs<T>(object value)
        {
            T result = default(T);

            if (value != null && (typeof(T) != value.GetType()))
            {
                if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                {
                    value = Convert.ToInt32(value);
                }
                else if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
                {
                    value = Convert.ToBoolean(value);
                }
                else if (typeof(T).IsEnum)
                {
                    object enumValue;
                    if (TryParseEnum<T>(typeof(T), value.ToString(), out enumValue))
                    {
                        value = (T)enumValue;
                    }
                }
            }
            else if (value != null && (typeof(T).IsEnum))
            {
                object enumValue;
                if (TryParseEnum<T>(typeof(T), value.ToString(), out enumValue))
                {
                    value = enumValue;
                }
            }
            result = value != null ? (T)value : default(T);

            return result;
        }

        /// <summary>
        /// This method exists because in NetStandard the Enum.TryParse methods that accept in a type
        /// are not present, and the generic TryParse method requires the type T to be non-nullable which
        /// is hard to check. This is different to the NetCore definition for some reason.
        /// It seems easier to implement our own than work around this.
        /// </summary>
        private static bool TryParseEnum<T>(Type t, string value, out object enumValue)
        {
            try
            {
                enumValue = Enum.Parse(t, value);
                return true;
            }
            catch(Exception)
            {
                enumValue = default(T);
                return false;
            }
        }

        protected void SetOptionValue<T>(string name, T value)
        {
            Options = Options ?? new Dictionary<string, object>();
            if (Options.ContainsKey(name))
            {
                Options[name] = value;
            }
            else
            {
                Options.Add(name, value);
            }
        }

        /// <summary>
        /// Gets or Sets the options
        /// </summary>
        public Dictionary<string, object> Options { get; set; }
    }
}
