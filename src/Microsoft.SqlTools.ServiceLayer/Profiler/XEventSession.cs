//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.XEvent;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Wrapper class for SMO XEvent Session that provides a consistent interface
    /// for managing Extended Events sessions on SQL Server.
    /// </summary>
    public class XEventSession : IXEventSession
    {
        /// <summary>
        /// Gets or sets the underlying SMO XEvent Session object.
        /// </summary>
        public Session Session { get; set; }

        private SessionId sessionId;

        /// <summary>
        /// Gets the unique identifier for this XEvent session.
        /// Lazily initialized on first access.
        /// </summary>
        public SessionId Id
        {
            get { return sessionId ??= GetSessionId(); }
        }

        /// <summary>
        /// Creates and returns a unique session identifier based on the server name and session ID.
        /// </summary>
        /// <returns>A SessionId combining the server name and numeric session ID</returns>
        protected virtual SessionId GetSessionId()
        {
            return new SessionId($"{Session.Parent.Name}_{Session.ID}", Session?.ID);
        }

        /// <summary>
        /// Starts the XEvent session on the SQL Server.
        /// </summary>
        public virtual void Start()
        {
            this.Session.Start();
        }

        /// <summary>
        /// Stops the XEvent session on the SQL Server.
        /// </summary>
        public virtual void Stop()
        {
            this.Session.Stop();
        }
    }
}
