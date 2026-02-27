//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Extended Events session monitor interface
    /// </summary>
    public interface IProfilerSessionMonitor
    {
        /// <summary>
        /// Starts monitoring an Extended Events session
        /// </summary>
        bool StartMonitoringSession(string viewerId, IXEventSession session);

        /// <summary>
        /// Stops monitoring an Extended Events session
        /// </summary>
        bool StopMonitoringSession(string viewerId, out ProfilerSession session);

        /// <summary>
        /// Pauses or Unpauses the stream of events to the viewer.
        /// Returns true if the viewer was found and toggled, false otherwise.
        /// </summary>
        /// <param name="viewerId">The viewer identifier</param>
        /// <param name="isPaused">When successful, contains the new pause state (true = paused, false = active)</param>
        /// <returns>True if the viewer was found and toggled, false otherwise</returns>
        bool PauseViewer(string viewerId, out bool isPaused);
    }
}
