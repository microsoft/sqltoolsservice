//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.XEvent.XELite;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Class to monitor active profiler sessions
    /// </summary>
    public class ProfilerSessionMonitor : IProfilerSessionMonitor
    {
        private object sessionsLock = new object();

        private object listenersLock = new object();

        private Task processorThread = null;

        private struct Viewer
        {
            public string Id { get; set; }
            public bool active { get; set; }

            public int xeSessionId { get; set; }

            public Viewer(string Id, bool active, int xeId)
            {
                this.Id = Id;
                this.active = active;
                this.xeSessionId = xeId;
            }
        };

        // XEvent Session Id's matched to the Profiler Id's watching them
        private Dictionary<int, List<string>> sessionViewers = new Dictionary<int, List<string>>();

        // XEvent Session Id's matched to their Profiler Sessions
        private Dictionary<int, ProfilerSession> monitoredSessions = new Dictionary<int, ProfilerSession>();

        // XEvent Session Id's matched to their stream cancellation tokens 
        private Dictionary<int, CancellationTokenSource> monitoredCancellationTokenSources = new Dictionary<int, CancellationTokenSource>();

        // ViewerId -> Viewer objects
        private Dictionary<string, Viewer> allViewers = new Dictionary<string, Viewer>();

        private List<IProfilerSessionListener> listeners = new List<IProfilerSessionListener>();

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

        public void StartMonitoringSession(string viewerId, IXEventSession session)
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
                    var profilerSession = new ProfilerSession();
                    profilerSession.XEventSession = session;

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
                    viewers = new List<string> { viewerId };
                    sessionViewers.Add(session.Id, viewers);
                }
            }
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

        private bool RemoveSession(int sessionId, out ProfilerSession session)
        {
            lock (this.sessionsLock)
            {
                //cancel running XEventStream for session.
                CancellationTokenSource targetToken;
                if (monitoredCancellationTokenSources.Remove(sessionId, out targetToken))
                {
                    targetToken.Cancel();
                }
                if (this.monitoredSessions.Remove(sessionId, out session))
                {
                    session.IsStreaming = false;

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

        /// <summary>
        /// The core queue processing method, cycles through monitored sessions and creates a stream for them if not already.
        /// </summary>
        private void ProcessSessions()
        {
            while (true)
            {
                lock (this.sessionsLock)
                {
                    foreach (var id in this.monitoredSessions.Keys)
                    {
                        ProfilerSession session;
                        this.monitoredSessions.TryGetValue(id, out session);
                        if (!session.IsStreaming)
                        {
                            List<string> viewers = this.sessionViewers[session.XEventSession.Id];
                            if (viewers.Any(v => allViewers[v].active)){
                                StartStream(id, session);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper function used to process the XEvent feed from a session's stream.
        /// </summary>
        private async Task HandleXEvent(IXEvent xEvent, ProfilerSession session)
        {
            
            ProfilerEvent profileEvent = new ProfilerEvent(xEvent.Name, xEvent.Timestamp.ToString());
            foreach (var kvp in xEvent.Fields)
            {
                profileEvent.Values.Add(kvp.Key, kvp.Value.ToString());
            }
            foreach (var kvp in xEvent.Actions)
            {
                profileEvent.Values.Add(kvp.Key, kvp.Value.ToString());
            }
            var eventList = new List<ProfilerEvent>();
            eventList.Add(profileEvent);

            if (eventList.Count > 0)
            {
                //session.FilterOldEvents(eventList); - Remove filter old events as it expects oldevents all the time in order to function correctly, this stream does not provide that.
                eventList = session.FilterProfilerEvents(eventList);
                // notify all viewers of the event.
                List<string> viewerIds = this.sessionViewers[session.XEventSession.Id];

                foreach (string viewerId in viewerIds)
                {
                    if (allViewers[viewerId].active)
                    {
                        SendEventsToListeners(viewerId, eventList, false);
                    }
                }
            }
        }

        /// <summary>
        /// Function that creates a brand new stream from a session, this is called from ProcessSessions when a session doesn't have a stream running currently.
        /// </summary>
        private void StartStream(int id, ProfilerSession session)
        {
            if(session.XEventSession != null  && session.XEventSession.Session != null && session.XEventSession.ConnectionDetails != null){
                CancellationTokenSource threadCancellationToken = new CancellationTokenSource();
                 //initial catalog must be set to master for XElite stream, otherwise it will not function.
                 //XElite stream is currently not supported on Azure Databases as read event stream is not available on them.
                var connectionString = ConnectionService.BuildConnectionString(session.XEventSession.ConnectionDetails);
                connectionString = Regex.Replace(connectionString, "Initial Catalog\\=[a-zA-Z0-9]+;", "Initial Catalog=master;");
                var eventStreamer = new XELiveEventStreamer(connectionString, session.XEventSession.Session.Name);
                // Start streaming task here, will run until cancellation or error with the feed.
                var task = eventStreamer.ReadEventStream(xEvent => HandleXEvent(xEvent, session), threadCancellationToken.Token);

                task.ContinueWith(t =>
                {
                    //If cancellation token is missing, that means stream was stopped by the client, do not notify in this case.
                    CancellationTokenSource targetToken;
                    if (monitoredCancellationTokenSources.TryGetValue(id, out targetToken))
                    {
                        StopSession(session.XEventSession.Id);
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);

                this.monitoredCancellationTokenSources.Add(id, threadCancellationToken);
                session.IsStreaming = true;
            }
            else {
                ProfilerSession tempSession;
                RemoveSession(id, out tempSession);
                throw new Exception(SR.SessionMissingDetails(id));
            }
        }

        /// <summary>
        /// Helper function for notifying listeners and stopping session in case the session is stopped on the server. This is public for tests.  
        /// </summary>
        public void StopSession(int Id){
            SendStoppedSessionInfoToListeners(Id);
            ProfilerSession tempSession;
            RemoveSession(Id, out tempSession);
        }

        /// <summary>
        /// Notify listeners about closed sessions
        /// </summary>
        private void SendStoppedSessionInfoToListeners(int sessionId)
        {
            lock (listenersLock)
            {
                foreach (var listener in this.listeners)
                {
                    foreach (string viewerId in sessionViewers[sessionId])
                    {
                        listener.SessionStopped(viewerId, sessionId);
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
