using Microsoft.SqlServer.XEvent.XELite;
using Microsoft.SqlServer.XEvent.XELite.Internal;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Wrapper XEventSession for IXEventFetcher instances
    /// </summary>
    class ObservableXEventSession : XEventSession, IObservableXEventSession
    {
        private readonly XeStreamObservable observableSession;
        private readonly SessionId sessionId;
        public IObservable<ProfilerEvent> ObservableSessionEvents => observableSession;
        
        public override void Start()
        {
            Session?.Start();
            observableSession.Start();
        }

        public override void Stop()
        {
            observableSession.Close();
            Session?.Stop();
        }

        public ObservableXEventSession(IXEventFetcher xeventFetcher, SessionId sessionId)
        {
            observableSession = new XeStreamObservable(xeventFetcher);
            this.sessionId = sessionId;
        }

        protected override SessionId GetSessionId()
        {
            return sessionId;
        }
    }

    /// <summary>
    /// Source of ProfilerEvent push notifications. Wraps IXEventFetcher.
    /// </summary>
    public class XeStreamObservable : IObservable<ProfilerEvent>
    {
        private readonly object syncObj = new object();
        private readonly List<IObserver<ProfilerEvent>> observers = new List<IObserver<ProfilerEvent>>();
        private CancellationTokenSource cancellationTokenSource;
        private readonly IXEventFetcher xeventFetcher;

        /// <summary>
        /// Constructs a new XeStreamObservable that converts xevent data from the fetcher to ProfilerEvent instances
        /// </summary>
        /// <param name="fetcher"></param>
        public XeStreamObservable(IXEventFetcher fetcher)
        {
            xeventFetcher = fetcher;
        }

        /// <summary>
        /// Starts processing xevents from the source.
        /// </summary>
        public void Start()
        {
            cancellationTokenSource = new CancellationTokenSource();
            var xeventFetcherTask = xeventFetcher.ReadEventStream(OnEventRead, cancellationTokenSource.Token);
            xeventFetcherTask.ContinueWith(OnStreamClosed);
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
