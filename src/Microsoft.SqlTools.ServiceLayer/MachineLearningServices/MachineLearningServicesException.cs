//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.MachineLearningServices
{
    /// <summary>
    /// Exception raised from machine learning services operations
    /// </summary>
    public class MachineLearningServicesException : Exception
    {
        internal MachineLearningServicesException() : base()
        {
        }

        internal MachineLearningServicesException(string m) : base(m)
        {
        }

        internal MachineLearningServicesException(string m, Exception innerException) : base(m, innerException)
        {
        }
    }
}
