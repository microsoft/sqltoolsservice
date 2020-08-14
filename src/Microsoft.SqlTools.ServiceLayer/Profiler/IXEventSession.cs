//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;

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
        SessionId Id { get; }

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

    public interface IObservableXEventSession : IXEventSession
    {
        IObservable<Contracts.ProfilerEvent> ObservableSessionEvents { get; }
    }

    /// <summary>
    /// Strong type for use as a dictionary key, simply wraps a string.
    /// Using a class helps distinguish instances from other strings like viewer id.
    /// </summary>
    public class SessionId
    {
        private readonly string sessionId;
        // SQL Server starts session counters at around 64k, so it's unlikely that this process-scoped counter would collide with a real session id
        // Eventually the profiler extension in ADS will use the string instead of the number.
        private static int numericIdCurrent = -1;
        /// <summary>
        /// Constructs a new sessionId 
        /// </summary>
        /// <param name="sessionId">The true unique identifier string, opaque to the client.</param>
        /// <param name="numericId">An optional numeric identifier used to identify the session to older clients that don't consume the string yet</param>
        public SessionId(string sessionId, int? numericId = null)
        {
            this.sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            NumericId = numericId ?? Interlocked.Increment(ref numericIdCurrent);
        }

        public int NumericId { get; private set; }

        public override int GetHashCode() => sessionId.GetHashCode();

        public override bool Equals(object obj)
        {
            return (obj is SessionId id) && id.sessionId.Equals(sessionId);
        }

        public override string ToString()
        {
            return sessionId;
        }
    }
}
