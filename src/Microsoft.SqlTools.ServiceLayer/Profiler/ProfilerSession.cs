//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Profiler session class
    /// </summary>
    public class ProfilerSession : IDisposable
    {
        private object pollingLock = new object();
        private bool isPolling = false;
        private readonly SessionObserver sessionObserver;
        private readonly IXEventSession xEventSession;
        private readonly IDisposable observerDisposable;
        private readonly Action<ProfilerSession> onSessionActivity;

        /// <summary>
        /// Connection to use for the session
        /// </summary>
        public ConnectionInfo ConnectionInfo { get; set; }

        /// <summary>
        /// Constructs a new ProfilerSession to watch the given IXeventSession's incoming events
        /// </summary>
        /// <param name="xEventSession">The XEvent session to monitor</param>
        /// <param name="onSessionActivity">Optional callback invoked when events arrive or session completes/errors</param>
        public ProfilerSession(IXEventSession xEventSession, Action<ProfilerSession> onSessionActivity = null)
        {
            this.xEventSession = xEventSession;
            this.onSessionActivity = onSessionActivity;
            if (xEventSession is IObservableXEventSession observableSession)
            {
                // For push-based sessions, subscribe to the event stream
                observerDisposable = observableSession.ObservableSessionEvents?.Subscribe(
                    sessionObserver = new SessionObserver(OnSessionActivity));
            }
        }

        /// <summary>
        /// Callback when session activity occurs (events received, completed, or error).
        /// </summary>
        private void OnSessionActivity()
        {
            onSessionActivity?.Invoke(this);
        }

        /// <summary>
        /// Underlying XEvent session wrapper
        /// </summary>
        public IXEventSession XEventSession => xEventSession;

        /// <summary>
        /// Try to enter processing mode (prevents concurrent processing of same session)
        /// </summary>
        /// <returns>True if entered processing mode, False if already processing</returns>
        public bool TryEnterProcessing()
        {
            lock (this.pollingLock)
            {
                if (!this.isPolling)
                {
                    this.isPolling = true;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Exit processing mode
        /// </summary>
        public void ExitProcessing()
        {
            lock (this.pollingLock)
            {
                this.isPolling = false;
            }
        }

        /// <summary>
        /// Is the session currently being processed
        /// </summary>
        public bool IsProcessing
        {
            get
            {
                lock (this.pollingLock)
                {
                    return this.isPolling;
                }
            }
        }



        /// <summary>
        /// Indicates if the current session has completed processing and will provide no new events
        /// </summary>
        public bool Completed
        {
            get
            {
                return sessionObserver?.Completed ?? false;
            }
        }

        /// <summary>
        /// Provides any fatal error encountered when processing a session
        /// </summary>
        public Exception Error => sessionObserver?.Error;

        /// <summary>
        /// Returns the current set of events buffered in memory since the last call.
        /// Events are pushed by XELite and buffered in the SessionObserver.
        /// </summary>
        public IEnumerable<ProfilerEvent> GetCurrentEvents()
        {
            if (sessionObserver == null)
            {
                return Enumerable.Empty<ProfilerEvent>();
            }
            return sessionObserver.CurrentEvents;
        }

        public void Dispose()
        {
            observerDisposable?.Dispose();
        }
    }

    [DebuggerDisplay("SessionObserver. Current:{writeBuffer.Count} Total:{eventCount}")]
    class SessionObserver : IObserver<ProfilerEvent>
    {
        private List<ProfilerEvent> writeBuffer = new List<ProfilerEvent>();
        private Int64 eventCount = 0;
        private readonly Action onEventReceived;

        public SessionObserver(Action onEventReceived = null)
        {
            this.onEventReceived = onEventReceived;
        }

        public void OnCompleted()
        {
            Completed = true;
            onEventReceived?.Invoke();
        }

        public void OnError(Exception error)
        {
            Error = error;
            Completed = true;
            onEventReceived?.Invoke();
        }

        public void OnNext(ProfilerEvent value)
        {
            writeBuffer.Add(value);
            eventCount++;
            // Notify that a new event has arrived
            onEventReceived?.Invoke();
        }

        public bool Completed { get; private set; }

        public Exception Error { get; private set; }

        public IEnumerable<ProfilerEvent> CurrentEvents
        {
            get
            {
                var newBuffer = new List<ProfilerEvent>();
                var oldBuffer = Interlocked.Exchange(ref writeBuffer, newBuffer);
                return oldBuffer;
            }
        }
    }
}
