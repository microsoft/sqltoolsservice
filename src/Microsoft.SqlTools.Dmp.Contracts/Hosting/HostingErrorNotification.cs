//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Dmp.Contracts;

namespace Microsoft.SqlTools.Dmp.Contracts.Hosting
{
    /// <summary>
    /// Parameters to be used for reporting hosting-level errors, such as protocol violations
    /// </summary>
    public class HostingErrorParams
    {
        /// <summary>
        /// The message of the error
        /// </summary>
        public string Message { get; set; }
    }

    public class HostingErrorNotification
    {
        public static readonly 
            EventType<HostingErrorParams> Type =
            EventType<HostingErrorParams>.Create("hosting/error");

    }
}
