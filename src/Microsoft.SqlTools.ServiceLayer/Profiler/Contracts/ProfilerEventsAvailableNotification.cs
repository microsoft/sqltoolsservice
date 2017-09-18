//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    public class ProfilerEventsAvailableParams
    {
        public string SessionId { get; set; }

        public List<ProfilerEvent> Events { get; set; }
    }

    public class ProfilerEventsAvailableNotification
    {
        public static readonly
            EventType<ProfilerEventsAvailableParams> Type =
            EventType<ProfilerEventsAvailableParams>.Create("profiler/eventsavailable");
    }
}


