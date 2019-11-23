//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Xml;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Profiler session monitor interface
    /// </summary>
    public interface IProfilerSessionMonitor
    {
        /// <summary>
        /// Starts monitoring a profiler session
        /// </summary>
        bool StartMonitoringSession(string viewerId, IXEventSession session);

        /// <summary>
        /// Stops monitoring a profiler session
        /// </summary>
        bool StopMonitoringSession(string viewerId, out ProfilerSession session);

        /// <summary>
        /// Pauses or Unpauses the stream of events to the viewer
        /// </summary>
        void PauseViewer(string viewerId);
    }
}
