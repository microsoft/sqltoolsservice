//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Profiler session class
    /// </summary>
    public class ProfilerSession
    {
        private static readonly TimeSpan DefaultPollingDelay = TimeSpan.FromSeconds(1);
        private object pollingLock = new object();
        private bool isPolling = false;
        private DateTime lastPollTime = DateTime.Now.Subtract(DefaultPollingDelay);
        private TimeSpan pollingDelay = DefaultPollingDelay;
        private ProfilerEvent lastSeenEvent = null;

        public bool pollImmediatly = false;

        /// <summary>
        /// Connection to use for the session
        /// </summary>
        public ConnectionInfo ConnectionInfo { get; set; }

        /// <summary>
        /// Underlying XEvent session wrapper
        /// </summary>
        public IXEventSession XEventSession { get; set; }

        /// <summary>
        /// Try to set the session into polling mode if criteria is meet
        /// </summary>
        /// <returns>True if session set to polling mode, False otherwise</returns>
        public bool TryEnterPolling()
        {
            lock (this.pollingLock)
            {
                if (pollImmediatly || (!this.isPolling && DateTime.Now.Subtract(this.lastPollTime) >= pollingDelay))
                {
                    this.isPolling = true;
                    this.lastPollTime = DateTime.Now;
                    this.pollImmediatly = false;
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
        public TimeSpan PollingDelay
        {
            get
            {
                return pollingDelay;
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
                return currentEvent.Values["batch_text"].Contains("SELECT target_data FROM sys.dm_xe_session_targets");
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
            if (lastSeenEvent != null)
            {
                // find the last event we've previously seen
                bool foundLastEvent = false;
                int idx = events.Count;
                while (--idx >= 0)
                {
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

                // save the last event so we know where to clean-up the list from next time
                if (events.Count > 0)
                {
                    lastSeenEvent = events.LastOrDefault();
                }
            }
            else    // first poll at start of session, all data is old
            {
                // save the last event as the beginning of the profiling session
                if (events.Count > 0)
                {
                    lastSeenEvent = events.LastOrDefault();
                }

                // ignore all events before the session began
                events.Clear();
            }
        }
    }
}
