//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


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
    }
}
