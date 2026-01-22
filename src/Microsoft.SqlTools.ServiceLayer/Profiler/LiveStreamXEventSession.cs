//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.XEvent.XELite;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// XEvent session that uses XELite's XELiveEventStreamer for push-based live event streaming.
    /// 
    /// This class combines two components:
    /// - Session: The XEvent Session object used for session lifecycle management (start/stop the session on SQL Server)
    /// - observableSession: The LiveStreamObservable that wraps XELite for push-based event streaming
    /// 
    /// The Session object is needed because XELite only handles event streaming from an existing session;
    /// it does not manage session lifecycle on the server. The Session object provides the ability to
    /// start and stop the XEvent session on SQL Server.
    /// </summary>
    public class LiveStreamXEventSession : IObservableXEventSession
    {
        /// <summary>
        /// Observable wrapper that uses XELite for push-based event delivery.
        /// This handles the actual event streaming from SQL Server.
        /// </summary>
        private readonly LiveStreamObservable observableSession;
        private readonly SessionId sessionId;

        /// <summary>
        /// Gets or sets the underlying XEvent Session object used for session lifecycle management.
        /// This is used to start/stop the XEvent session on SQL Server.
        /// XELite only streams events; it cannot manage session state.
        /// </summary>
        public Session Session { get; set; }

        /// <summary>
        /// Gets or sets the SQL connection used for XEvent session management.
        /// This connection is disposed when the session is stopped.
        /// </summary>
        public Microsoft.Data.SqlClient.SqlConnection SqlConnection { get; set; }

        /// <summary>
        /// Gets the unique identifier for this XEvent session.
        /// </summary>
        public SessionId Id => sessionId;

        /// <summary>
        /// Gets the observable stream of profiler events for push-based delivery.
        /// </summary>
        public IObservable<ProfilerEvent> ObservableSessionEvents => observableSession;

        /// <summary>
        /// Creates a new LiveStreamXEventSession for the specified XEvent session.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <param name="sessionName">Name of the XEvent session to stream from</param>
        /// <param name="sessionId">Unique session identifier</param>
        /// <param name="maxReconnectAttempts">Maximum reconnection attempts on failure (default: 3)</param>
        /// <param name="reconnectDelay">Initial delay between reconnection attempts (default: 1 second)</param>
        public LiveStreamXEventSession(
            string connectionString,
            string sessionName,
            SessionId sessionId,
            int maxReconnectAttempts = ProfilerConstants.DefaultMaxReconnectAttempts,
            TimeSpan? reconnectDelay = null)
            : this(() => new XELiveEventStreamer(connectionString, sessionName), sessionId, maxReconnectAttempts, reconnectDelay)
        {
        }

        /// <summary>
        /// Creates a new LiveStreamXEventSession with a custom streamer factory (for testing).
        /// </summary>
        public LiveStreamXEventSession(
            Func<IXEventFetcher> streamerFactory,
            SessionId sessionId,
            int maxReconnectAttempts = ProfilerConstants.DefaultMaxReconnectAttempts,
            TimeSpan? reconnectDelay = null)
        {
            this.sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            observableSession = new LiveStreamObservable(
                streamerFactory ?? throw new ArgumentNullException(nameof(streamerFactory)),
                maxReconnectAttempts,
                reconnectDelay);
        }

        /// <summary>
        /// Starts the live event stream. The underlying XEvent session should already be running.
        /// </summary>
        public void Start()
        {
            // Ensure the underlying XEvent session is running before starting the stream
            if (Session != null && !Session.IsRunning)
            {
                Session.Start();
            }
            observableSession.Start();
        }

        /// <summary>
        /// Stops the live event stream and optionally the underlying session.
        /// </summary>
        public void Stop()
        {
            observableSession.Close();
            Session?.Stop();
            SqlConnection?.Dispose();
        }
    }

    /// <summary>
    /// Observable wrapper for XELiveEventStreamer that provides push-based event delivery
    /// with automatic reconnection on transient failures.
    /// 
    /// Note: This class implements its own reconnection logic rather than relying on ReliableConnection
    /// because XELite manages its own internal SQL connection for event streaming. The reconnection
    /// here handles XELite stream interruptions (network blips, server restarts, session timeouts)
    /// which are distinct from the SQL connection used for session management. ReliableConnection
    /// cannot intercept or retry XELite's internal connection failures.
    /// </summary>
    internal class LiveStreamObservable : IObservable<ProfilerEvent>
    {
        private readonly object syncObj = new object();
        private readonly List<IObserver<ProfilerEvent>> observers = new List<IObserver<ProfilerEvent>>();
        private readonly Func<IXEventFetcher> streamerFactory;
        private readonly int maxReconnectAttempts;
        private readonly TimeSpan baseReconnectDelay;
        private CancellationTokenSource cancellationTokenSource;
        private int currentReconnectAttempt;
        private bool isClosed;

        /// <summary>
        /// Creates a new LiveStreamObservable.
        /// </summary>
        /// <param name="streamerFactory">Factory function to create XELiveEventStreamer instances</param>
        /// <param name="maxReconnectAttempts">Maximum reconnection attempts (0 = no retries)</param>
        /// <param name="baseReconnectDelay">Base delay between reconnection attempts (uses exponential backoff)</param>
        public LiveStreamObservable(
            Func<IXEventFetcher> streamerFactory,
            int maxReconnectAttempts = ProfilerConstants.DefaultMaxReconnectAttempts,
            TimeSpan? baseReconnectDelay = null)
        {
            this.streamerFactory = streamerFactory ?? throw new ArgumentNullException(nameof(streamerFactory));
            this.maxReconnectAttempts = Math.Max(0, maxReconnectAttempts);
            this.baseReconnectDelay = baseReconnectDelay ?? ProfilerConstants.DefaultReconnectDelay;
        }

        /// <summary>
        /// Starts streaming events from the XEvent session.
        /// </summary>
        public void Start()
        {
            lock (syncObj)
            {
                if (isClosed)
                {
                    throw new InvalidOperationException("Cannot restart a closed stream");
                }
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new CancellationTokenSource();
                currentReconnectAttempt = 0;
            }
            StartStreamInternal();
        }

        /// <summary>
        /// Stops the event stream and notifies all observers of completion.
        /// </summary>
        public void Close()
        {
            lock (syncObj)
            {
                if (isClosed) return;
                isClosed = true;
                cancellationTokenSource?.Cancel();
            }
            NotifyCompleted();
        }

        /// <summary>
        /// Subscribes an observer to receive profiler events.
        /// </summary>
        public IDisposable Subscribe(IObserver<ProfilerEvent> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            
            lock (syncObj)
            {
                if (!observers.Contains(observer))
                {
                    observers.Add(observer);
                }
            }
            return new Unsubscriber(this, observer);
        }

        private void StartStreamInternal()
        {
            // Fire-and-forget: start the async streaming task without blocking
            _ = StartStreamAsync();
        }

        private async Task StartStreamAsync()
        {
            IXEventFetcher fetcher;
            try
            {
                fetcher = streamerFactory();
            }
            catch (Exception ex)
            {
                // Handle synchronous exceptions from factory - no fetcher to dispose
                HandleFactoryException(ex);
                return;
            }

            try
            {
                var token = cancellationTokenSource.Token;
                await fetcher.ReadEventStream(OnEventReceived, token);
                // Stream completed normally
                OnStreamEnded(completed: true, exception: null, fetcher: fetcher);
            }
            catch (OperationCanceledException)
            {
                // Stream was cancelled (expected during Stop)
                (fetcher as IDisposable)?.Dispose();
                OnStreamEnded(completed: true, exception: null, fetcher: null);
            }
            catch (Exception ex)
            {
                // Stream failed with an error
                OnStreamEnded(completed: false, exception: ex, fetcher: fetcher);
            }
        }

        private void HandleFactoryException(Exception ex)
        {
            if (currentReconnectAttempt < maxReconnectAttempts)
            {
                ScheduleReconnect();
                return;
            }

            // Max retries exceeded - notify error and complete
            NotifyError(ex);
            NotifyCompleted();
        }

        private Task OnEventReceived(IXEvent xEvent)
        {
            // Reset reconnect counter on successful event delivery
            currentReconnectAttempt = 0;

            var profilerEvent = ConvertToProfilerEvent(xEvent);
            
            // Deliver event immediately to all observers (true push model)
            foreach (var observer in GetCurrentObservers())
            {
                try
                {
                    observer.OnNext(profilerEvent);
                }
                catch
                {
                    // Don't let one observer's error affect others
                }
            }

            return Task.CompletedTask;
        }

        private static ProfilerEvent ConvertToProfilerEvent(IXEvent xEvent)
        {
            // Use the XEvent UUID if available, otherwise generate a new one
            var uuid = xEvent.UUID != Guid.Empty ? xEvent.UUID : Guid.NewGuid();

            // Extract event_sequence from actions if available
            long? eventSequence = null;
            if (xEvent.Actions != null && 
                xEvent.Actions.TryGetValue("event_sequence", out var seqValue) && 
                seqValue != null &&
                long.TryParse(seqValue.ToString(), out var seq))
            {
                eventSequence = seq;
            }

            var profilerEvent = new ProfilerEvent(xEvent.Name, xEvent.Timestamp.ToString(), uuid, eventSequence);

            // Add fields
            foreach (var kvp in xEvent.Fields)
            {
                var value = kvp.Value?.ToString() ?? string.Empty;
                if (!profilerEvent.Values.ContainsKey(kvp.Key))
                {
                    profilerEvent.Values.Add(kvp.Key, value);
                }
            }

            // Add actions (with collision handling)
            if (xEvent.Actions != null)
            {
                foreach (var kvp in xEvent.Actions)
                {
                    var key = kvp.Key;
                    var value = kvp.Value?.ToString() ?? string.Empty;
                    
                    if (profilerEvent.Values.ContainsKey(key))
                    {
                        key += " (action)";
                    }
                    profilerEvent.Values.Add(key, value);
                }
            }

            return profilerEvent;
        }

        private void OnStreamEnded(bool completed, Exception exception, IXEventFetcher fetcher)
        {
            // Dispose the fetcher if it implements IDisposable
            (fetcher as IDisposable)?.Dispose();

            // Check if we were intentionally closed
            bool wasClosed;
            lock (syncObj)
            {
                wasClosed = isClosed || cancellationTokenSource?.IsCancellationRequested == true;
            }

            if (wasClosed)
            {
                NotifyCompleted();
                return;
            }

            // Handle stream failure with reconnection
            if (exception != null)
            {
                if (currentReconnectAttempt < maxReconnectAttempts)
                {
                    ScheduleReconnect();
                    return;
                }

                // Max retries exceeded - notify error and complete
                NotifyError(exception);
            }

            NotifyCompleted();
        }

        private void ScheduleReconnect()
        {
            currentReconnectAttempt++;

            // Exponential backoff with jitter (using thread-safe Random.Shared)
            var delay = TimeSpan.FromMilliseconds(
                baseReconnectDelay.TotalMilliseconds * Math.Pow(2, currentReconnectAttempt - 1) *
                (0.5 + Random.Shared.NextDouble() * 0.5));

            var token = cancellationTokenSource?.Token ?? CancellationToken.None;

            // Fire-and-forget: schedule reconnection after delay
            _ = ReconnectAfterDelayAsync(delay, token);
        }

        private async Task ReconnectAfterDelayAsync(TimeSpan delay, CancellationToken token)
        {
            try
            {
                await Task.Delay(delay, token);
                await StartStreamAsync();
            }
            catch (OperationCanceledException)
            {
                // Reconnection was cancelled - this is expected during Stop()
            }
        }

        private List<IObserver<ProfilerEvent>> GetCurrentObservers()
        {
            lock (syncObj)
            {
                return [.. observers];
            }
        }

        private void NotifyCompleted()
        {
            foreach (var observer in GetCurrentObservers())
            {
                try
                {
                    observer.OnCompleted();
                }
                catch
                {
                    // Ignore observer errors during completion
                }
            }
        }

        private void NotifyError(Exception exception)
        {
            foreach (var observer in GetCurrentObservers())
            {
                try
                {
                    observer.OnError(exception);
                }
                catch
                {
                    // Ignore observer errors during error notification
                }
            }
        }

        private void RemoveObserver(IObserver<ProfilerEvent> observer)
        {
            lock (syncObj)
            {
                observers.Remove(observer);
            }
        }

        private class Unsubscriber : IDisposable
        {
            private readonly LiveStreamObservable parent;
            private readonly IObserver<ProfilerEvent> observer;

            public Unsubscriber(LiveStreamObservable parent, IObserver<ProfilerEvent> observer)
            {
                this.parent = parent;
                this.observer = observer;
            }

            public void Dispose()
            {
                parent?.RemoveObserver(observer);
            }
        }
    }
}
