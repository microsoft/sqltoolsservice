//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Profiler session class
    /// </summary>
    public class ProfilerSession : IDisposable
    {
        private static readonly TimeSpan DefaultPollingDelay = TimeSpan.FromSeconds(1);
        private object pollingLock = new object();
        private bool isPolling = false;
        private DateTime lastPollTime = DateTime.Now.Subtract(DefaultPollingDelay);
        private ProfilerEvent lastSeenEvent = null;
        private readonly SessionObserver sessionObserver;
        private readonly IXEventSession xEventSession;
        private readonly IDisposable observerDisposable;
        private bool eventsLost = false;
        int lastSeenId = -1;

        public bool pollImmediately = false;

        /// <summary>
        /// Connection to use for the session
        /// </summary>
        public ConnectionInfo ConnectionInfo { get; set; }

        /// <summary>
        /// Constructs a new ProfilerSession to watch the given IXeventSession's incoming events
        /// </summary>
        /// <param name="xEventSession"></param>
        public ProfilerSession(IXEventSession xEventSession)
        {
            this.xEventSession = xEventSession;
            if (xEventSession is IObservableXEventSession observableSession)
            {
                observerDisposable = observableSession.ObservableSessionEvents?.Subscribe(sessionObserver = new SessionObserver());
            }
        }    

        /// <summary>
        /// Underlying XEvent session wrapper
        /// </summary>
        public IXEventSession XEventSession => xEventSession;

        /// <summary>
        /// Try to set the session into polling mode if criteria is meet
        /// </summary>
        /// <returns>True if session set to polling mode, False otherwise</returns>
        public bool TryEnterPolling()
        {
            lock (this.pollingLock)
            {
                if (pollImmediately || (!this.isPolling && DateTime.Now.Subtract(this.lastPollTime) >= PollingDelay))
                {
                    this.isPolling = true;
                    this.lastPollTime = DateTime.Now;
                    this.pollImmediately = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Is the session currently being polled
        /// </summary>
        public bool IsPolling
        {
            get
            {
                return this.isPolling;
            }
            set
            {
                lock (this.pollingLock)
                {
                    this.isPolling  = value;
                }
            }
        }

        /// <summary>
        /// The delay between session polls
        /// </summary>
        public TimeSpan PollingDelay { get; } = DefaultPollingDelay;

        /// <summary>
        /// Could events have been lost in the last poll
        /// </summary>
        public bool EventsLost
        {
            get
            {
                return this.eventsLost;
            }
        }

        /// <summary>
        /// Determine if an event was caused by the XEvent polling queries
        /// </summary>
        private bool IsProfilerEvent(ProfilerEvent currentEvent)
        {
            if (string.IsNullOrWhiteSpace(currentEvent.Name) ||  currentEvent.Values == null)
            {
                return false;
            }

            if ((currentEvent.Name.Equals("sql_batch_completed")
                || currentEvent.Name.Equals("sql_batch_starting"))
                && currentEvent.Values.ContainsKey("batch_text"))
            {
                return currentEvent.Values["batch_text"].Contains("SELECT target_data FROM sys.dm_xe_session_targets")
                    || currentEvent.Values["batch_text"].Contains("SELECT target_data FROM sys.dm_xe_database_session_targets");
            }

            return false;
        }

        /// <summary>
        /// Removed profiler polling events from event list
        /// </summary>
        public List<ProfilerEvent> FilterProfilerEvents(List<ProfilerEvent> events)
        {
            int idx = events.Count;
            while (--idx >= 0)
            {
                if (IsProfilerEvent(events[idx]))
                {
                    events.RemoveAt(idx);
                }
            }
            return events;
        }

        /// <summary>
        /// Filter the event list to not include previously seen events,
        /// and to exclude events that happened before the profiling session began.
        /// </summary>
        public void FilterOldEvents(List<ProfilerEvent> events)
        {
            this.eventsLost = false;
            
            if (lastSeenId != -1)
            {
                // find the last event we've previously seen
                bool foundLastEvent = false;
                int idx = events.Count;
                int earliestSeenEventId = int.Parse(events.LastOrDefault().Values["event_sequence"]);
                while (--idx >= 0)
                {
                    // update the furthest back event we've found so far
                    earliestSeenEventId = Math.Min(earliestSeenEventId, int.Parse(events[idx].Values["event_sequence"]));

                    if (events[idx].Equals(lastSeenEvent))
                    {
                        foundLastEvent = true;
                        break;
                    }
                }

                // remove all the events we've seen before
                if (foundLastEvent)
                {
                    events.RemoveRange(0, idx + 1);
                }
                else if(earliestSeenEventId > (lastSeenId + 1))
                {
                    // if there's a gap between the expected next event sequence
                    // and the furthest back event seen, we know we've lost events
                    this.eventsLost = true;
                }

                // save the last event so we know where to clean-up the list from next time
                if (events.Count > 0)
                {
                    lastSeenEvent = events.LastOrDefault();
                    lastSeenId = int.Parse(lastSeenEvent.Values["event_sequence"]);
                }
            }
            else    // first poll at start of session, all data is old
            {
                // save the last event as the beginning of the profiling session
                if (events.Count > 0)
                {
                    lastSeenEvent = events.LastOrDefault();
                    lastSeenId = int.Parse(lastSeenEvent.Values["event_sequence"]);
                }

                // ignore all events before the session began
                events.Clear();
            }
        }

        /// <summary>
        /// Indicates if the current session has completed processing and will provide no new events
        /// </summary>
        public bool Completed
        {
            get
            {
                return (sessionObserver != null) ? sessionObserver.Completed : error != null;
            }
        }

        private Exception error;
        /// <summary>
        /// Provides any fatal error encountered when processing a session
        /// </summary>
        public Exception Error
        {
            get
            {
                return sessionObserver?.Error ?? error;
            }
        }

        /// Returns the current set of events in the session buffer.
        /// For RingBuffer sessions, returns the content of the session ring buffer by querying the server.
        /// For LiveTarget and LocalFile sessions, returns the events buffered in memory since the last call to GetCurrentEvents.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ProfilerEvent> GetCurrentEvents()
        {
            if (XEventSession == null && sessionObserver == null)
            {
                return Enumerable.Empty<ProfilerEvent>();
            }
            if (sessionObserver == null)
            {
                try
                {
                    var targetXml = XEventSession.GetTargetXml();

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(targetXml);

                    var nodes = xmlDoc.DocumentElement.GetElementsByTagName("event");
                    var rawEvents = nodes.Cast<XmlNode>().Select(n => ParseProfilerEvent(n)).ToList();
                    FilterOldEvents(rawEvents);
                    return rawEvents;
                }
                catch (Exception e)
                {
                    error ??= e;
                    return Enumerable.Empty<ProfilerEvent>();
                }
            }
            return sessionObserver.CurrentEvents;
        }

        /// <summary>
        /// Parse a single event node from XEvent XML
        /// </summary>
        private static ProfilerEvent ParseProfilerEvent(XmlNode node)
        {
            var name = node.Attributes["name"];
            var timestamp = node.Attributes["timestamp"];

            var profilerEvent = new ProfilerEvent(name.InnerText, timestamp.InnerText);

            foreach (XmlNode childNode in node.ChildNodes)
            {
                var childName = childNode.Attributes["name"];
                XmlNode typeNode = childNode.SelectSingleNode("type");
                var typeName = typeNode.Attributes["name"];
                XmlNode valueNode = childNode.SelectSingleNode("value");

                if (!profilerEvent.Values.ContainsKey(childName.InnerText))
                {
                    profilerEvent.Values.Add(childName.InnerText, valueNode.InnerText);
                }
            }

            return profilerEvent;
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
        public void OnCompleted()
        {
            Completed = true;
        }

        public void OnError(Exception error)
        {
            Error = error;
        }

        public void OnNext(ProfilerEvent value)
        {
            writeBuffer.Add(value);
            eventCount++;
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
