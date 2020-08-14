//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Class to monitor active profiler sessions
    /// </summary>
    public class ProfilerSessionMonitor : IProfilerSessionMonitor
    {
        private const int PollingLoopDelay = 1000;

        private object sessionsLock = new object();

        private object listenersLock = new object();

        private object pollingLock = new object();

        private Task processorThread = null;

        private struct Viewer
        {
            public string Id { get; set; }
            public bool active { get; set; }

            public SessionId xeSessionId { get; set; }

            public Viewer(string Id, bool active, SessionId xeId)
            {
                this.Id = Id;
                this.active = active;
                this.xeSessionId = xeId;
            }
        };

        // XEvent Session Id's matched to the Profiler Id's watching them
        private readonly Dictionary<SessionId, List<string>> sessionViewers = new Dictionary<SessionId, List<string>>();

        // XEvent Session Id's matched to their Profiler Sessions
        private readonly Dictionary<SessionId, ProfilerSession> monitoredSessions = new Dictionary<SessionId, ProfilerSession>();

        // ViewerId -> Viewer objects
        private readonly Dictionary<string, Viewer> allViewers = new Dictionary<string, Viewer>();

        private readonly List<IProfilerSessionListener> listeners = new List<IProfilerSessionListener>();

        /// <summary>
        /// Registers a session event Listener to receive a callback when events arrive
        /// </summary>
        public void AddSessionListener(IProfilerSessionListener listener)
        {
            lock (this.listenersLock)
            {
                this.listeners.Add(listener);
            }
        }

        /// <summary>
        /// Start monitoring the provided session
        /// </summary>
        public bool StartMonitoringSession(string viewerId, IXEventSession session)
        {
            lock (this.sessionsLock)
            {
                // start the monitoring thread
                if (this.processorThread == null)
                {
                    this.processorThread = Task.Factory.StartNew(ProcessSessions);
                }

                // create new profiling session if needed
                if (!this.monitoredSessions.ContainsKey(session.Id))
                {
                    var profilerSession = new ProfilerSession(session);

                    this.monitoredSessions.Add(session.Id, profilerSession);
                }

                // create a new viewer, or configure existing viewer
                Viewer viewer;
                if (!this.allViewers.TryGetValue(viewerId, out viewer))
                {
                    viewer = new Viewer(viewerId, true, session.Id);
                    allViewers.Add(viewerId, viewer);
                }
                else
                {
                    viewer.active = true;
                    viewer.xeSessionId = session.Id;
                }

                // add viewer to XEvent session viewers
                List<string> viewers;
                if (this.sessionViewers.TryGetValue(session.Id, out viewers))
                {
                    viewers.Add(viewerId);
                }
                else
                {
                    viewers = new List<string>{ viewerId };
                    sessionViewers.Add(session.Id, viewers);
                }
            }

            return true;
        }

        /// <summary>
        /// Stop monitoring the session watched by viewerId
        /// </summary>
        public bool StopMonitoringSession(string viewerId, out ProfilerSession session)
        {
            lock (this.sessionsLock)
            {
                Viewer v;
                if (this.allViewers.TryGetValue(viewerId, out v))
                {
                    return RemoveSession(v.xeSessionId, out session);
                }
                else
                {
                    session = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Toggle the pause state for the viewer
        /// </summary>
        public void PauseViewer(string viewerId)
        {
            lock (this.sessionsLock)
            {
                Viewer v = this.allViewers[viewerId];
                v.active = !v.active;
                this.allViewers[viewerId] = v;
            }
        }

        private bool RemoveSession(SessionId sessionId, out ProfilerSession session)
        {
            lock (this.sessionsLock)
            {
                if (this.monitoredSessions.Remove(sessionId, out session))
                {
                    //remove all viewers for this session
                    List<string> viewerIds;
                    if (sessionViewers.Remove(sessionId, out viewerIds))
                    {
                        foreach (String viewerId in viewerIds)
                        {
                            this.allViewers.Remove(viewerId);
                        }
                        return true;
                    }
                    else
                    {
                        session = null;
                        return false;
                    }
                }
                else
                {
                    session = null;
                    return false;
                }
            }
        }

        public void PollSession(SessionId sessionId)
        {
            lock (this.sessionsLock)
            {
                this.monitoredSessions[sessionId].pollImmediately = true;
            }
            lock (this.pollingLock)
            {
                Monitor.Pulse(pollingLock);
            }
        }

        /// <summary>
        /// The core queue processing method
        /// </summary>
        /// <param name="state"></param>
        private void ProcessSessions()
        {
            while (true)
            {
                lock (this.pollingLock)
                {
                    lock (this.sessionsLock)
                    {
                        foreach (var session in this.monitoredSessions.Values)
                        {
                            List<string> viewers = this.sessionViewers[session.XEventSession.Id];
                            if (viewers.Any(v => allViewers[v].active))
                            {
                                ProcessSession(session);
                            }
                        }
                    }
                    Monitor.Wait(this.pollingLock, PollingLoopDelay);
                }
            }
        }

        /// <summary>
        /// Process a session for new XEvents if it meets the polling criteria
        /// </summary>
        private void ProcessSession(ProfilerSession session)
        {
            if (session.TryEnterPolling())
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var events = PollSession(session);
                        bool eventsLost = session.EventsLost;
                        if (events.Count > 0 || eventsLost)
                        {
                            // notify all viewers for the polled session
                            List<string> viewerIds = this.sessionViewers[session.XEventSession.Id];
                            foreach (string viewerId in viewerIds)
                            {
                                if (allViewers[viewerId].active)
                                {
                                    SendEventsToListeners(viewerId, events, eventsLost);
                                }
                            }
                        }                        
                    }
                    finally
                    {
                        session.IsPolling = false;
                    }
                    if (session.Completed)
                    {
                        SendStoppedSessionInfoToListeners(session.XEventSession.Id, session.Error.Message);
                        RemoveSession(session.XEventSession.Id, out ProfilerSession tempSession);
                        tempSession.Dispose();
                    }
                });
            }
        }

        private List<ProfilerEvent> PollSession(ProfilerSession session)
        {
            var events = new List<ProfilerEvent>();
            if (session == null || session.XEventSession == null)
            {
                return events;
            }

            events.AddRange(session.GetCurrentEvents());
            
            return session.FilterProfilerEvents(events);
        }

        /// <summary>
        /// Notify listeners about closed sessions
        /// </summary>
        private void SendStoppedSessionInfoToListeners(SessionId sessionId, string errorMessage)
        {
            lock (listenersLock)
            {
                foreach (var listener in this.listeners)
                {
                    foreach(string viewerId in sessionViewers[sessionId])
                    {
                        listener.SessionStopped(viewerId, sessionId, errorMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Notify listeners when new profiler events are available
        /// </summary>
        private void SendEventsToListeners(string sessionId, List<ProfilerEvent> events, bool eventsLost)
        {
            lock (listenersLock)
            {
                foreach (var listener in this.listeners)
                {
                    listener.EventsAvailable(sessionId, events, eventsLost);
                }
            }
        }

    }
}
