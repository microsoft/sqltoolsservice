//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Dmp.Contracts.Hosting
{
    /// <summary>
    /// Defines the message contract for errors returned via <see cref="Microsoft.SqlTools.Dmp.Hosting.Protocol.RequestContext.SendError"/>
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Error code. If omitted will default to 0
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }
    }
}