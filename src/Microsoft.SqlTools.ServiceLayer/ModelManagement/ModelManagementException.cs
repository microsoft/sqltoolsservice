//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement
{
    /// <summary>
    /// Exception raised from machine learning services operations
    /// </summary>
    public class ModelManagementException : Exception
    {
        internal ModelManagementException() : base()
        {
        }

        internal ModelManagementException(string m) : base(m)
        {
        }

        internal ModelManagementException(string m, Exception innerException) : base(m, innerException)
        {
        }
    }
}
