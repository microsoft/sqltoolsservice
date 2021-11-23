//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlServer.Management.XEvent;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Main class for Profiler Service functionality
    /// </summary>
    public interface IXEventSession
    {
        /// <summary>
        /// Gets unique XEvent session Id
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Connection details associated with the session id.
        /// </summary>
        ConnectionDetails ConnectionDetails { get; set; }

        /// <summary>
        /// Session associated with the session id.
        /// </summary>
        Session Session { get; set; }

        /// <summary>
        /// Starts XEvent session
        /// </summary>
        void Start();

        /// <summary>
        /// Stops XEvent session
        /// </summary>
        void Stop();

        /// <summary>
        /// Reads XEvent XML from the default session target
        /// </summary>
        string GetTargetXml();
    }
}
