//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensibility
{
    /// <summary>
    /// Exception raised from machine learning services operations
    /// </summary>
    public class LanguageExtensibilityException : Exception
    {
        internal LanguageExtensibilityException() : base()
        {
        }

        internal LanguageExtensibilityException(string m) : base(m)
        {
        }

        internal LanguageExtensibilityException(string m, Exception innerException) : base(m, innerException)
        {
        }
    }
}
