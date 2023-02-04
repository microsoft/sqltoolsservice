//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    public class ProfilerSessionStoppedParams
    {
        public string OwnerUri { get; set; }

        public int SessionId { get; set; }
    }

    public class ProfilerSessionStoppedNotification
    {
        public static readonly
            EventType<ProfilerSessionStoppedParams> Type =
            EventType<ProfilerSessionStoppedParams>.Create("profiler/sessionstopped");
    }
}
