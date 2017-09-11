//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    public class ProfilerSession
    {
        private bool isPolling = false;
        private DateTime lastPollTime = DateTime.Now;
        private TimeSpan pollingDelay = TimeSpan.FromSeconds(1);
        private List<ProfilerEvent> sessionEvents = new List<ProfilerEvent>();

        public string SessionId { get; set; }

        public ConnectionInfo ConnectionInfo { get; set; }

        public IXEventSession XEventSession { get; set; }

        public bool IsPolling 
        { 
            get
            {
                return this.isPolling;
            }
            set
            {
                // if we are switching isPolling from false to true then reset lastPollTime
                if (!this.isPolling && value)
                {
                    this.lastPollTime = DateTime.Now;
                }
                this.isPolling = value;
            }
        }

        public TimeSpan PollingDelay 
        {
            get
            {
                return pollingDelay;
            }
        }

        public bool ShouldPoll
        {
            get
            {
                return !this.IsPolling && 
                    DateTime.Now.Subtract(this.lastPollTime) >= pollingDelay; 
            }
        }

        public List<ProfilerEvent> FilterOldEvents(List<ProfilerEvent> events)
        {
            return null;
        }
    }
}
