//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.XEvent.XELite;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// XEvent session for reading local .xel files using XELite's XEFileEventStreamer.
    /// Unlike LiveStreamXEventSession, this does not connect to a SQL Server instance.
    /// </summary>
    internal class LocalFileXEventSession : IObservableXEventSession
    {
        private readonly LocalFileObservable observableSession;
        private readonly SessionId sessionId;

        /// <summary>
        /// Gets the unique identifier for this session (the file path).
        /// </summary>
        public SessionId Id => sessionId;

        /// <summary>
        /// Gets the observable stream of profiler events from the .xel file.
        /// </summary>
        public IObservable<ProfilerEvent> ObservableSessionEvents => observableSession;
        
        /// <summary>
        /// Starts reading events from the .xel file.
        /// </summary>
        public void Start()
        {
            observableSession.Start();
        }

        /// <summary>
        /// Stops reading events from the .xel file.
        /// </summary>
        public void Stop()
        {
            observableSession.Close();
        }

        /// <summary>
        /// Creates a new LocalFileXEventSession for reading the specified .xel file.
        /// </summary>
        /// <param name="xeventFetcher">Factory to create the XEFileEventStreamer</param>
        /// <param name="sessionId">Session identifier (typically the file path)</param>
        public LocalFileXEventSession(Func<IXEventFetcher> xeventFetcher, SessionId sessionId)
        {
            observableSession = new LocalFileObservable(xeventFetcher);
            this.sessionId = sessionId;
        }
    }

    /// <summary>
    /// Observable wrapper for XEFileEventStreamer that reads .xel files and delivers ProfilerEvents.
    /// </summary>
    internal class LocalFileObservable : IObservable<ProfilerEvent>
    {
        private readonly object syncObj = new object();
        private readonly List<IObserver<ProfilerEvent>> observers = new List<IObserver<ProfilerEvent>>();
        private CancellationTokenSource cancellationTokenSource;
        private readonly Func<IXEventFetcher> xeventFetcher;

        /// <summary>
        /// Creates a new LocalFileObservable that reads events from an .xel file.
        /// </summary>
        /// <param name="fetcher">Factory to create the XEFileEventStreamer</param>
        public LocalFileObservable(Func<IXEventFetcher> fetcher)
        {
            xeventFetcher = fetcher;
        }

        /// <summary>
        /// Starts processing xevents from the source.
        /// </summary>
        public void Start()
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                var xeventFetcherFuncCallBack = xeventFetcher();
                var xeventFetcherTask = xeventFetcherFuncCallBack.ReadEventStream(OnEventRead, cancellationTokenSource.Token);
                xeventFetcherTask.ContinueWith(OnStreamClosed);
            } catch (Exception ex)
            {
                Task.FromException<IXEventFetcher>(ex).ContinueWith(OnStreamClosed);
            }
            
        }

        /// <summary>
        /// Stops the xevent fetching task and informs all listeners that the event stream has ended and clears the list of listeners.
        /// Start could be called again, but only new subscribers will see the data.
        /// </summary>
        public void Close()
        {
            cancellationTokenSource.Cancel();
            var currentObservers = CurrentObservers;
            currentObservers.ForEach(o => o.OnCompleted());
            lock (syncObj)
            {
                currentObservers.ForEach(o => observers.Remove(o));
            }
        }

        /// <summary>
        /// Adds the observer to the listener list
        /// </summary>
        /// <param name="observer"></param>
        /// <returns>An IDisposable for the listener to call when it no longer wishes to receive events</returns>
        public IDisposable Subscribe(IObserver<ProfilerEvent> observer)
        {
            lock (syncObj)
            {
                if (!observers.Contains(observer))
                {
                    observers.Add(observer);
                }
                return new Unsubscriber(observers, observer);
            }
        }

        private List<IObserver<ProfilerEvent>> CurrentObservers
        {
            get
            {
                lock (syncObj)
                {
                    return new List<IObserver<ProfilerEvent>>(observers);
                }
            }
        }

        private void OnStreamClosed(Task fetcherTask)
        {
            if (fetcherTask.IsFaulted)
            {
                CurrentObservers.ForEach(o => o.OnError(fetcherTask.Exception));
            }
            Close();
        }

        private Task OnEventRead(IXEvent xEvent)
        {
            ProfilerEvent profileEvent = new ProfilerEvent(xEvent.Name, xEvent.Timestamp.ToString());
            foreach (var kvp in xEvent.Fields)
            {
                profileEvent.Values.Add(kvp.Key, kvp.Value.ToString());
            }
            // Add the XE 'actions'.
            if(xEvent.Actions != null)
            {
                foreach (var kvp in xEvent.Actions)
                {
                    string key = kvp.Key;
                    if (profileEvent.Values.ContainsKey(key))
                    {
                        // Append a postfix to avoid duplicate keys while keeping the data.
                        key += " (action)";
                    }
                    profileEvent.Values.Add(key, kvp.Value.ToString());
                }
            }
            CurrentObservers.ForEach(o => o.OnNext(profileEvent));
            return Task.FromResult(0);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<ProfilerEvent>> _observers;
            private readonly IObserver<ProfilerEvent> _observer;

            public Unsubscriber(List<IObserver<ProfilerEvent>> observers, IObserver<ProfilerEvent> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                {
                    _observers.Remove(_observer);
                }
            }
        }
    }
}
