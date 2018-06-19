//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    public interface IProfilerSessionListener
    {
        void EventsAvailable(string sessionId, List<ProfilerEvent> events, bool eventsLost);

        void SessionStopped(string viewerId, int sessionId);
    }
}
