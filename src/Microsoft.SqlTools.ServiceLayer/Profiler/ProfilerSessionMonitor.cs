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

        private Dictionary<string, ProfilerSession> monitoredSessions = new Dictionary<string, ProfilerSession>();

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
        public bool StartMonitoringSession(ProfilerSession session)
        {
            lock (this.sessionsLock)
            {
                // start the monitoring thread 
                if (this.processorThread == null)
                {
                    this.processorThread = Task.Factory.StartNew(ProcessSessions);;
                }

                if (!this.monitoredSessions.ContainsKey(session.SessionId))
                {
                    this.monitoredSessions.Add(session.SessionId, session);
                }
            }

            return true;
        }

        /// <summary>
        /// Stop monitoring the session specified by the sessionId
        /// </summary>
        public bool StopMonitoringSession(string sessionId)
        {
            lock (this.sessionsLock)
            {
                if (this.monitoredSessions.ContainsKey(sessionId))
                {
                    ProfilerSession session;
                    return this.monitoredSessions.Remove(sessionId, out session);
                }
                else
                {
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
                        ProcessSession(session);
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
                        SendEventsToListeners(session.SessionId, events);
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

            return session.FilterProfilerEvents(
                session.FilterOldEvents(events)
            );
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
