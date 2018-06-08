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
    /// Classs to monitor active profiler sessions
    /// </summary>
    public class ProfilerSessionMonitor : IProfilerSessionMonitor
    {
        private const int PollingLoopDelay = 1000;

        private object sessionsLock = new object();

        private object listenersLock = new object();

        private Task processorThread = null;

        public struct Viewer
        {
            public string ID;
            public bool active;

            public int xeSessionID;

            public Viewer(string ID, bool active, int xeID)
            {
                this.ID = ID;
                this.active = active;
                this.xeSessionID = xeID;
            }
        };

        // XEvent Session ID's matched to the Profiler ID's watching them
        private Dictionary<int, List<string>> sessionViewers = new Dictionary<int, List<string>>();

        // XEvent Session ID's matched to their Profiler Sessions
        private Dictionary<int, ProfilerSession> monitoredSessions = new Dictionary<int, ProfilerSession>();

        // ViewerID -> Viewer objects
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
        /// Start monitoring the provided sessions
        /// </summary>
        public bool StartMonitoringSession(string viewerID, IXEventSession session)
        {
            lock (this.sessionsLock)
            {
                // start the monitoring thread
                if (this.processorThread == null)
                {
                    this.processorThread = Task.Factory.StartNew(ProcessSessions);
                }

                // create new profiling session if needed
                if (!this.monitoredSessions.ContainsKey(session.ID))
                {
                    var profilerSession = new ProfilerSession();
                    profilerSession.XEventSession = session;

                    this.monitoredSessions.Add(session.ID, profilerSession);
                }

                // create a new viewer, or configure existing viewer
                Viewer viewer;
                if(!this.allViewers.TryGetValue(viewerID, out viewer))
                {
                    viewer = new Viewer(viewerID, true, session.ID);
                    allViewers.Add(viewerID, viewer);
                }
                else
                {
                    viewer.active = true;
                    viewer.xeSessionID = session.ID;
                }

                // add viewer to XEvent session viewers
                List<string> viewers;
                if(this.sessionViewers.TryGetValue(session.ID, out viewers))
                {
                    viewers.Add(viewerID);
                }
                else
                {
                    viewers = new List<string>{ viewerID };
                    sessionViewers.Add(session.ID, viewers);
                }
            }

            return true;
        }

        // TODO: Clean this up
        /// <summary>
        /// Stop monitoring the session watched by viewerID
        /// </summary>
        public bool StopMonitoringSession(string viewerID, out ProfilerSession session)
        {
            lock (this.sessionsLock)
            {
                Viewer v;
                if(this.allViewers.TryGetValue(viewerID, out v))
                {
                    return RemoveSession(v.xeSessionID, out session);
                }
                else
                {
                    session = null;
                    return false;
                }
            }
        }

        public void PauseViewer(string viewerID)
        {
            //This is called both to pause & unpause viewers
            lock (this.sessionsLock)
            {
                //update the status in all viewers
                Viewer v = this.allViewers[viewerID];
                this.allViewers[viewerID] = new Viewer(viewerID, !v.active, v.xeSessionID);

                //update the viewer in the session viewer's list
                //viewer = this.sessionViewers[viewer.xeSessionID].Find(v => v.ID == viewerID);
                //viewer.active = !viewer.active;
            }
        }

        private bool RemoveSession(int sessionID, out ProfilerSession session)
        {
            lock (this.sessionsLock)
            {
                if(this.monitoredSessions.Remove(sessionID, out session))
                {
                    //remove all viewers for this session
                    List<string> viewerIDs;
                    if(sessionViewers.Remove(sessionID, out viewerIDs))
                    {
                        foreach(String vID in viewerIDs)
                        {
                            //TODO: Notify users that the session has stopped
                            this.allViewers.Remove(vID);
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
        /// The core queue processing method
        /// </summary>
        /// <param name="state"></param>
        private void ProcessSessions()
        {
            while (true)
            {
                lock (this.sessionsLock)
                {
                    foreach (var session in this.monitoredSessions.Values)
                    {
                        List<string> viewers = this.sessionViewers[session.XEventSession.ID];
                        if(viewers.Any(v => allViewers[v].active))
                        {
                            ProcessSession(session);
                        }
                    }
                }

                Thread.Sleep(PollingLoopDelay);
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
                    var events = PollSession(session);
                    if (events.Count > 0)
                    {
                        // notify all viewers for the polled session
                        List<string> viewerIDs = this.sessionViewers[session.XEventSession.ID];
                        foreach(string vID in viewerIDs)
                        {
                            if(allViewers[vID].active)
                            {
                                SendEventsToListeners(vID, events);
                            }
                        }
                    }
                });
            }
        }

        private List<ProfilerEvent> PollSession(ProfilerSession session)
        {
            var events = new List<ProfilerEvent>();
            try
            {
                if (session == null || session.XEventSession == null)
                {
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
            catch(Exception ex)
            {
                Logger.Write(LogLevel.Warning, "Failed to pool session. error: " + ex.Message);
            }
            finally
            {
                session.IsPolling = false;
            }

            session.FilterOldEvents(events);
            return session.FilterProfilerEvents(events);
        }

        /// <summary>
        /// Notify listeners when new profiler events are available
        /// </summary>
        private void SendEventsToListeners(string sessionId, List<ProfilerEvent> events)
        {
            lock (listenersLock)
            {
                foreach (var listener in this.listeners)
                {
                    listener.EventsAvailable(sessionId, events);
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
