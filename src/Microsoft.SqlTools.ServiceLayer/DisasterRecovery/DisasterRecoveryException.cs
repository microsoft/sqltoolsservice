//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{

    /// <summary>
    /// Exception raised from disaster recovery operations
    /// </summary>
    internal sealed class DisasterRecoveryException : Exception
    {
        internal DisasterRecoveryException() : base()
        {
        }

        internal DisasterRecoveryException(string m) : base(m)
        {
        }
    }
}