//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.Test.Common.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Normalizes line endings, replacing all \r\n with \n.
        /// </summary>
        /// <param name="str">The string</param>
        /// <returns>The string with all line endings normalized</returns>
        public static string NormalizeLineEndings(this string str)
        {
            return str.Replace("\r\n", "\n");
        }
    }
}
