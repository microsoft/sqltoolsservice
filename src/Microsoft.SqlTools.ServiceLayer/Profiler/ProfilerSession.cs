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
        /// Indicates whether there are pending events in the buffer that haven't been retrieved yet.
        /// </summary>
        public bool HasPendingEvents => sessionObserver?.HasPendingEvents ?? false;

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

    /// <summary>
    /// Observer that receives profiler events from an IObservable stream and buffers them
    /// for immediate delivery to the ProfilerSessionMonitor.
    /// Implements the observer pattern to support push-based XELite streaming, where events
    /// are delivered to clients as soon as they arrive.
    /// </summary>
    [DebuggerDisplay("SessionObserver. Current:{writeBuffer.Count} Total:{eventCount}")]
    class SessionObserver : IObserver<ProfilerEvent>
    {
        /// <summary>
        /// Buffer that accumulates incoming events until they are retrieved via CurrentEvents.
        /// Uses lock-free swap pattern for thread-safe access.
        /// </summary>
        private List<ProfilerEvent> writeBuffer = new List<ProfilerEvent>();

        /// <summary>
        /// Total count of events received since the observer was created (for diagnostics).
        /// </summary>
        private Int64 eventCount = 0;

        /// <summary>
        /// Optional callback invoked when events arrive, complete, or error occurs.
        /// Triggers immediate processing in the ProfilerSessionMonitor (push-based delivery).
        /// </summary>
        private readonly Action onEventReceived;

        /// <summary>
        /// Creates a new SessionObserver.
        /// </summary>
        /// <param name="onEventReceived">Optional callback invoked when events are available or stream ends</param>
        public SessionObserver(Action onEventReceived = null)
        {
            this.onEventReceived = onEventReceived;
        }

        /// <summary>
        /// Called when the observable stream completes normally (e.g., session stopped).
        /// Marks the observer as completed and notifies the polling loop.
        /// </summary>
        public void OnCompleted()
        {
            Completed = true;
            onEventReceived?.Invoke();
        }

        /// <summary>
        /// Called when the observable stream encounters an error (e.g., connection lost after max retries).
        /// Stores the error, marks completion, and notifies the polling loop.
        /// </summary>
        /// <param name="error">The exception that caused the stream to fail</param>
        public void OnError(Exception error)
        {
            Error = error;
            Completed = true;
            onEventReceived?.Invoke();
        }

        /// <summary>
        /// Called for each profiler event received from the XELite stream.
        /// Adds the event to the write buffer and notifies the polling loop.
        /// Note: XELite guarantees sequential event delivery on a single thread,
        /// so no synchronization is needed for writeBuffer access in this method.
        /// </summary>
        /// <param name="value">The profiler event received from the stream</param>
        public void OnNext(ProfilerEvent value)
        {
            writeBuffer.Add(value);
            eventCount++;
            // Notify that a new event has arrived
            onEventReceived?.Invoke();
        }

        /// <summary>
        /// Indicates whether the observable stream has completed (normally or with error).
        /// </summary>
        public bool Completed { get; private set; }

        /// <summary>
        /// Contains the error if the stream terminated due to an exception, null otherwise.
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// Indicates whether there are pending events in the buffer that haven't been retrieved yet.
        /// </summary>
        public bool HasPendingEvents => writeBuffer.Count > 0;

        /// <summary>
        /// Retrieves and clears all buffered events using a lock-free swap pattern.
        /// A new empty buffer is swapped in atomically, and the old buffer's contents are returned.
        /// This allows concurrent writes (OnNext) and reads (CurrentEvents) without blocking.
        /// </summary>
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
