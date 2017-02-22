//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    internal static class ObjectExtensions
    {
        /// <summary>
        /// Extension to evaluate an object's ToString() method in an exception safe way. This will
        /// extension method will not throw.
        /// </summary>
        /// <param name="obj">The object on which to call ToString()</param>
        /// <returns>The ToString() return value or a suitable error message is that throws.</returns>
        public static string SafeToString(this object obj)
        {
            string str;

            try
            {
                str = obj.ToString();
            }
            catch (Exception ex)
            {
                str = $"<Error converting poperty value to string - {ex.Message}>";
            }

            return str;
        }

        /// <summary>
        /// Converts a boolean to a "1" or "0" string. Particularly helpful when sending telemetry
        /// </summary>
        public static string ToOneOrZeroString(this bool isTrue)
        {
            return isTrue ? "1" : "0";
        }
    }
}
