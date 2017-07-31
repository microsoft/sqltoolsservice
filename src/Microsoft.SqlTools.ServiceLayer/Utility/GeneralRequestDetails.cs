//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public class GeneralRequestDetails
    {
        public GeneralRequestDetails()
        {
            Options = new Dictionary<string, object>();
        }

        internal T GetOptionValue<T>(string name)
        {
            T result = default(T);
            if (Options != null && Options.ContainsKey(name))
            {
                object value = Options[name];
                try
                {
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
                    }
                    result = value != null ? (T)value : default(T);
                }
                catch
                {
                    result = default(T);
                    Logger.Write(LogLevel.Warning, string.Format(CultureInfo.InvariantCulture,
                        "Cannot convert option value {0}:{1} to {2}", name, value ?? "", typeof(T)));
                }
            }
            return result;
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
