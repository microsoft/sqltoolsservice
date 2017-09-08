//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Xml;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    public interface IProfilerSessionMonitor
    {
        bool StartMonitoringSession(ProfilerSession session);

        bool StopMonitoringSession(string sessionId);
    }
}
