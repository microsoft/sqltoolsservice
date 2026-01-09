//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.XEvent.XELite;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    public class TestSessionListener : IProfilerSessionListener
    {
        public readonly Dictionary<string, List<ProfilerEvent>> AllEvents = new Dictionary<string, List<ProfilerEvent>>();

        public readonly List<string> StoppedSessions = new List<string>();
        public readonly List<string> ErrorMessages = new List<string>();

        public void EventsAvailable(string sessionId, List<ProfilerEvent> events)
        {
            if (!AllEvents.ContainsKey(sessionId))
            {
                AllEvents[sessionId] = new List<ProfilerEvent>();
            }
            AllEvents[sessionId].AddRange(events);            
        }

        public void SessionStopped(string viewerId, SessionId sessionId, string errorMessage)
        {
            StoppedSessions.Add(viewerId);
            ErrorMessages.Add(errorMessage);
        }
    }

    public class TestXEventSession : IXEventSession
    {
        public SessionId Id { get { return new SessionId("testsession_51"); } }

        public void Start(){}

        public void Stop() { }
    }

    public class TestXEventSession1 : IXEventSession
    {
        public SessionId Id { get { return new SessionId("testsession_1"); } }

        public void Start(){}

        public void Stop() { }
    }

        public class TestXEventSession2 : IXEventSession
    {
        public SessionId Id { get { return new SessionId("testsession_2"); } }

        public void Start(){}

        public void Stop() { }
    }

    public class TestXEventSessionFactory : IXEventSessionFactory
    {
        private int sessionNum = 1;
        public IXEventSession GetXEventSession(string sessionName, ConnectionInfo connInfo)
        {
            if(sessionNum == 1)
            {
                sessionNum = 2;
                return new TestXEventSession1();
            }
            else
            {
                sessionNum = 1;
                return new TestXEventSession2();
            }
        }

        public IXEventSession CreateXEventSession(string createStatement, string sessionName, ConnectionInfo connInfo)
        {
            if(sessionNum == 1)
            {
                sessionNum = 2;
                return new TestXEventSession1();
            }
            else
            {
                sessionNum = 1;
                return new TestXEventSession2();
            }
        }

        public IXEventSession OpenLocalFileSession(string filePath)
        {
            throw new NotImplementedException();
        }
    }

    #region Live Streaming Test Helpers

    /// <summary>
    /// Test implementation of IXEvent for live streaming tests.
    /// </summary>
    public class TestXEvent : IXEvent
    {
        public string Name { get; }
        public Guid UUID { get; } = Guid.NewGuid();
        public DateTimeOffset Timestamp { get; }
        public IReadOnlyDictionary<string, object> Fields { get; }
        public IReadOnlyDictionary<string, object> Actions { get; }

        public TestXEvent(string name, DateTimeOffset timestamp, Dictionary<string, object> fields, Dictionary<string, object> actions = null)
        {
            Name = name;
            Timestamp = timestamp;
            Fields = fields;
            Actions = actions;
        }
    }

    /// <summary>
    /// Test implementation of IXEventFetcher for live streaming tests.
    /// Simulates XELite's XELiveEventStreamer behavior.
    /// </summary>
    public class TestLiveEventFetcher : IXEventFetcher
    {
        private readonly IEnumerable<IXEvent> events;
        private readonly bool failImmediately;
        private readonly int failAfterEvents;
        private readonly Exception exceptionToThrow;
        private readonly TimeSpan delayBetweenEvents;
        private readonly bool keepStreamOpen;

        public TestLiveEventFetcher(
            IEnumerable<IXEvent> events,
            bool failImmediately = false,
            Exception exceptionToThrow = null,
            TimeSpan? delayBetweenEvents = null,
            int failAfterEvents = -1,
            bool keepStreamOpen = false)
        {
            this.events = events ?? Array.Empty<IXEvent>();
            this.failImmediately = failImmediately;
            this.failAfterEvents = failAfterEvents;
            this.exceptionToThrow = exceptionToThrow ?? new Exception("Simulated stream failure");
            this.delayBetweenEvents = delayBetweenEvents ?? TimeSpan.Zero;
            this.keepStreamOpen = keepStreamOpen;
        }

        public Task ReadEventStream(HandleXEvent eventCallback, CancellationToken cancellationToken)
        {
            if (failImmediately)
            {
                return Task.FromException(exceptionToThrow);
            }

            return Task.Run(async () =>
            {
                int eventCount = 0;
                foreach (var evt in events)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (delayBetweenEvents > TimeSpan.Zero)
                    {
                        await Task.Delay(delayBetweenEvents, cancellationToken);
                    }

                    await eventCallback(evt);
                    eventCount++;

                    // Fail after delivering specified number of events
                    if (failAfterEvents > 0 && eventCount >= failAfterEvents)
                    {
                        throw exceptionToThrow;
                    }
                }

                // If keepStreamOpen is true, wait indefinitely until cancellation
                if (keepStreamOpen)
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
            }, cancellationToken);
        }

        public Task ReadEventStream(HandleMetadata metadataCallback, HandleXEvent eventCallback, CancellationToken cancellationToken)
        {
            // For testing, we ignore metadata callback and just use events
            return ReadEventStream(eventCallback, cancellationToken);
        }
    }

    /// <summary>
    /// Test factory for creating LiveStreamXEventSession instances with controlled fetchers.
    /// </summary>
    public class TestLiveStreamSessionFactory : IXEventSessionFactory
    {
        private readonly Queue<IXEventFetcher> fetcherQueue;
        private readonly int maxReconnectAttempts;
        private readonly TimeSpan reconnectDelay;

        public SessionId LastSessionId { get; private set; }
        public int FetchersCreated { get; private set; }

        public TestLiveStreamSessionFactory(
            IEnumerable<IXEventFetcher> fetchers,
            int maxReconnectAttempts = 3,
            TimeSpan? reconnectDelay = null)
        {
            fetcherQueue = new Queue<IXEventFetcher>(fetchers ?? Array.Empty<IXEventFetcher>());
            this.maxReconnectAttempts = maxReconnectAttempts;
            this.reconnectDelay = reconnectDelay ?? TimeSpan.FromMilliseconds(10);
        }

        public IXEventSession GetXEventSession(string sessionName, ConnectionInfo connInfo)
        {
            return CreateLiveStreamSession(sessionName);
        }

        public IXEventSession CreateXEventSession(string createStatement, string sessionName, ConnectionInfo connInfo)
        {
            return CreateLiveStreamSession(sessionName);
        }

        public IXEventSession OpenLocalFileSession(string filePath)
        {
            throw new NotImplementedException();
        }

        public LiveStreamXEventSession CreateLiveStreamSession(string sessionName)
        {
            LastSessionId = new SessionId($"test_{sessionName}_{FetchersCreated}");
            FetchersCreated++;

            return new LiveStreamXEventSession(
                GetNextFetcher,
                LastSessionId,
                maxReconnectAttempts,
                reconnectDelay);
        }

        private IXEventFetcher GetNextFetcher()
        {
            if (fetcherQueue.Count == 0)
            {
                throw new InvalidOperationException("No more test fetchers available");
            }
            return fetcherQueue.Dequeue();
        }
    }

    #endregion
}
