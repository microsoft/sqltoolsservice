//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.Profiler.Contracts
{
    public class ProfilerEventsAvailableParams
    {
        public string OwnerUri { get; set; }

        public List<ProfilerEvent> Events { get; set; }

        public bool EventsLost { get; set; }
    }

    public class ProfilerEventsAvailableNotification
    {
        public static readonly
            EventType<ProfilerEventsAvailableParams> Type =
            EventType<ProfilerEventsAvailableParams>.Create("profiler/eventsavailable");
    }
}
