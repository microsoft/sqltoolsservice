//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.Utility;

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

        // ViewerId -> Viewer objects
        private Dictionary<string, Viewer> allViewers = new Dictionary<string, Viewer>();

        private List<IProfilerSessionListener> listeners = new List<IProfilerSessionListener>();

        /// <summary>
        /// Registers a session event listener to receive a callback when events arrive
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

        private bool RemoveSession(int sessionId, out ProfilerSession session)
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

        public void PollSession(int sessionId)
        {
            lock (this.sessionsLock)
            {
                this.monitoredSessions[sessionId].pollImmediatly = true;
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
                    bool eventsLost;
                    var events = PollSession(session, out eventsLost);
                    if (events.Count > 0)
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
                });
            }
        }

        private List<ProfilerEvent> PollSession(ProfilerSession session, out bool eventsLost)
        {
            var events = new List<ProfilerEvent>();
            try
            {
                if (session == null || session.XEventSession == null)
                {
                    eventsLost = false;
                    return events;
                }

                var targetXml = session.XEventSession.GetTargetXml();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(targetXml);

                var nodes = xmlDoc.DocumentElement.GetElementsByTagName("event");
                foreach (XmlNode node in nodes)
                {
                    var profilerEvent = ParseProfilerEvent(node);
                    if (profilerEvent != null)
                    {
                        events.Add(profilerEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Warning, "Failed to pool session. error: " + ex.Message);
            }
            finally
            {
                session.IsPolling = false;
            }

            eventsLost = session.FilterOldEvents(events);
            return session.FilterProfilerEvents(events);
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

        /// <summary>
        /// Parse a single event node from XEvent XML
        /// </summary>
        private ProfilerEvent ParseProfilerEvent(XmlNode node)
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
    }
}
