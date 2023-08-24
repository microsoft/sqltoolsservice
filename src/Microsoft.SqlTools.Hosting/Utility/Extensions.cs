//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.Utility
{
    public static class ObjectExtensions
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

    public static class NullableExtensions
    {
        /// <summary>
        /// Extension method to evaluate a bool? and determine if it has the value and is true.
        /// This way we avoid throwing if the bool? doesn't have a value.
        /// </summary>
        /// <param name="obj">The <c>bool?</c> to process</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> has a value and it is <c>true</c>
        /// <c>false</c> otherwise.
        /// </returns>
        public static bool HasTrue(this bool? obj)
        {
            return obj.HasValue && obj.Value;
        }
    }

    public static class ExceptionExtensions
    {
        /// <summary>
        /// Returns true if the passed exception or any inner exception is an OperationCanceledException instance.
        /// </summary>
        public static bool IsOperationCanceledException(this Exception e)
        {
            Exception current = e;
            while (current != null)
            {
                if (current is OperationCanceledException)
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }

        public static string GetFullErrorMessage(this Exception e, bool includeStackTrace = false)
        {
            List<string> errors = new List<string>();

            while (e != null)
            {
                errors.Add(e.Message);
                if (includeStackTrace)
                {
                    errors.Add(e.StackTrace);
                }
                e = e.InnerException;
            }

            return errors.Count > 0 ? string.Join(includeStackTrace ? Environment.NewLine : " ---> ", errors) : string.Empty;
        }
    }
}
