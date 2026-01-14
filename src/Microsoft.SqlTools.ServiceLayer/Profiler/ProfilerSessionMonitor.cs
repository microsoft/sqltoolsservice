//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Class to monitor active profiler sessions using push-based event delivery
    /// </summary>
    public class ProfilerSessionMonitor : IProfilerSessionMonitor
    {
        private object sessionsLock = new object();

        private object listenersLock = new object();

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
                // create new profiling session if needed
                if (!this.monitoredSessions.ContainsKey(session.Id))
                {
                    // Create ProfilerSession with callback to process events when they arrive
                    var profilerSession = new ProfilerSession(session, OnSessionActivity);

                    this.monitoredSessions.Add(session.Id, profilerSession);

                    // Start observable sessions to begin event streaming
                    if (session is IObservableXEventSession)
                    {
                        session.Start();
                    }
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
        /// Callback invoked when a session has activity (events, completion, or error)
        /// </summary>
        private void OnSessionActivity(ProfilerSession session)
        {
            ProcessSession(session);
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
        /// <param name="viewerId">The viewer identifier</param>
        /// <param name="isPaused">When successful, contains the new pause state (true = paused, false = active)</param>
        /// <returns>True if the viewer was found and toggled, false otherwise</returns>
        public bool PauseViewer(string viewerId, out bool isPaused)
        {
            isPaused = false;

            if (string.IsNullOrEmpty(viewerId))
            {
                return false;
            }

            lock (this.sessionsLock)
            {
                if (this.allViewers.TryGetValue(viewerId, out Viewer v))
                {
                    v.active = !v.active;
                    this.allViewers[viewerId] = v;
                    isPaused = !v.active; // active=false means paused=true
                    return true;
                }
                return false;
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

        /// <summary>
        /// Process a session for new XEvents (push-based delivery)
        /// </summary>
        private void ProcessSession(ProfilerSession session)
        {
            if (session.TryEnterProcessing())
            {
                Task.Run(() =>
                {
                    try
                    {
                        int totalEventsSent = 0;
                        int emptyReadsInARow = 0;

                        // Keep processing until the session is complete and all events are drained.
                        // We use a counter to handle the race condition where events might still be
                        // arriving while we're checking for completion.
                        while (true)
                        {
                            var events = session.GetCurrentEvents().ToList();

                            if (events.Count > 0)
                            {
                                totalEventsSent += events.Count;
                                emptyReadsInARow = 0;

                                // notify all active viewers for the session
                                lock (this.sessionsLock)
                                {
                                    if (this.sessionViewers.TryGetValue(session.XEventSession.Id, out var viewerIds))
                                    {
                                        foreach (string viewerId in viewerIds)
                                        {
                                            if (allViewers.TryGetValue(viewerId, out var viewer) && viewer.active)
                                            {
                                                SendEventsToListeners(viewerId, events);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                emptyReadsInARow++;

                                if (session.Completed)
                                {
                                    // Session completed - check one more time for any late events
                                    // before signaling completion (to handle race conditions)
                                    if (emptyReadsInARow >= 2)
                                    {
                                        // Two empty reads in a row after completion - safe to exit
                                        SendStoppedSessionInfoToListeners(session.XEventSession.Id, session.Error?.Message);
                                        RemoveSession(session.XEventSession.Id, out ProfilerSession tempSession);
                                        tempSession?.Dispose();
                                        return;
                                    }
                                    // First empty read after completion - loop once more to be safe
                                }
                                else
                                {
                                    // No events and not completed - exit and wait for callback
                                    break;
                                }
                            }
                        }
                    }
                    finally
                    {
                        session.ExitProcessing();
                    }
                });
            }
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
                    if (sessionViewers.TryGetValue(sessionId, out var viewerIds))
                    {
                        foreach (string viewerId in viewerIds)
                        {
                            listener.SessionStopped(viewerId, sessionId, errorMessage);
                        }
                    }
                }
            }
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

    }
}
