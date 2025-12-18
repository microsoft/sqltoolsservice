//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable


using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Main class for Profiler Service functionality
    /// </summary>
    public interface IXEventSessionFactory
    {
        /// <summary>
        /// Gets an XEvent session with the given name
        /// </summary>
        IXEventSession GetXEventSession(string sessionName, ConnectionInfo connInfo);

        /// <summary>
        /// Creates an XEvent session with the given create statement and name
        /// </summary>
        IXEventSession CreateXEventSession(string createStatement, string sessionName, ConnectionInfo connInfo);

        /// <summary>
        /// Opens a session whose events are streamed from a local XEL file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        IXEventSession OpenLocalFileSession(string filePath);

        /// <summary>
        /// Opens a live streaming session using XELite's XELiveEventStreamer for push-based
        /// event delivery. This replaces SMO ring-buffer polling with direct event streaming.
        /// </summary>
        /// <param name="sessionName">Name of the XEvent session to stream from</param>
        /// <param name="connInfo">Connection information for the SQL Server</param>
        /// <returns>A live streaming XEvent session</returns>
        IXEventSession OpenLiveStreamSession(string sessionName, ConnectionInfo connInfo);
    }
}
