//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ResourceProvider.Core.Contracts
{
    /// <summary>
    /// Any response which relies on a token may indicated that the operation failed due to token being expired.
    /// All operational response messages should inherit from this class in order to support a standard method for defining
    /// this failure path
    /// </summary>
    public class TokenReliantResponse
    {
        /// <summary>
        /// Did this succeed?
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// If this failed, was it due to a token expiring?
        /// </summary>
        public bool IsTokenExpiredFailure { get; set; }
    }
}
