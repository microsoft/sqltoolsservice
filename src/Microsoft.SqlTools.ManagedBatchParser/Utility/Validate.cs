//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.BatchParser.Utility
{
    /// <summary>
    /// Provides common validation methods to simplify method
    /// parameter checks.
    /// </summary>
    public static class Validate
    {
        /// <summary>
        /// Throws ArgumentNullException if value is null.
        /// </summary>
        /// <param name="parameterName">The name of the parameter being validated.</param>
        /// <param name="valueToCheck">The value of the parameter being validated.</param>
        public static void IsNotNull(string parameterName, object valueToCheck)
        {
            if (valueToCheck == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Throws ArgumentException if the value is null or an empty string.
        /// </summary>
        /// <param name="parameterName">The name of the parameter being validated.</param>
        /// <param name="valueToCheck">The value of the parameter being validated.</param>
        public static void IsNotNullOrEmptyString(string parameterName, string valueToCheck)
        {
            if (string.IsNullOrEmpty(valueToCheck))
            {
                throw new ArgumentException(
                    "Parameter contains a null, empty, or whitespace string.",
                    parameterName);
            }
        }

        /// <summary>
        /// Throws ArgumentException if the value is null, an empty string,
        /// or a string containing only whitespace.
        /// </summary>
        /// <param name="parameterName">The name of the parameter being validated.</param>
        /// <param name="valueToCheck">The value of the parameter being validated.</param>
        public static void IsNotNullOrWhitespaceString(string parameterName, string valueToCheck)
        {
            if (string.IsNullOrWhiteSpace(valueToCheck))
            {
                throw new ArgumentException(
                    "Parameter contains a null, empty, or whitespace string.",
                    parameterName);
            }
        }
    }
}
