//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    public class ProfilerSessionStoppedParams
    {
        public string OwnerUri { get; set; }

        /// <summary>
        /// Numeric session id that is only unique on the server where the session resides
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// An key that uniquely identifies a session across all servers
        /// </summary>
        public string UniqueSessionId { get; set; }

        /// <summary>
        /// The error that stopped the session, if any.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    public class ProfilerSessionStoppedNotification
    {
        public static readonly
            EventType<ProfilerSessionStoppedParams> Type =
            EventType<ProfilerSessionStoppedParams>.Create("profiler/sessionstopped");
    }
}
