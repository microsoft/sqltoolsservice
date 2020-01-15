//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.ServerConfigurations
{
    /// <summary>
    /// Exception raised from machine learning services operations
    /// </summary>
    public class ServerConfigException : Exception
    {
        internal ServerConfigException() : base()
        {
        }

        internal ServerConfigException(string m) : base(m)
        {
        }

        internal ServerConfigException(string m, Exception innerException) : base(m, innerException)
        {
        }
    }
}
