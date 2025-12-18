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
    public static class ProfilerTestObjects
    {
        public static List<ProfilerEvent> TestProfilerEvents
        {
            get
            {
                ProfilerEvent event1 = new ProfilerEvent("event1", "1/1/2017");
                event1.Values.Add("event_sequence", "1");
                ProfilerEvent event2 = new ProfilerEvent("event2", "1/2/2017");
                event2.Values.Add("event_sequence", "2");
                ProfilerEvent event3 = new ProfilerEvent("event3", "1/3/2017");
                event3.Values.Add("event_sequence", "3");
                
                return new List<ProfilerEvent>
                {
                    event1,
                    event2,
                    event3
                };
            }
        }
    }

    public class TestSessionListener : IProfilerSessionListener
    {
        public readonly Dictionary<string, List<ProfilerEvent>> AllEvents = new Dictionary<string, List<ProfilerEvent>>();

        public readonly List<string> StoppedSessions = new List<string>();
        public readonly List<string> ErrorMessages = new List<string>();

        public void EventsAvailable(string sessionId, List<ProfilerEvent> events, bool eventsLost)
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
        private string testXEventXml =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>51</value>" +
            "		</data>" +
            "		<data name=\"is_dac\">" +
            "			<type name=\"boolean\" package=\"package0\"></type>" +
            "			<value>false</value>" +
            "		</data>" +
            "		<data name=\"database_id\">" +
            "			<type name=\"uint32\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "		<data name=\"packet_size\">" +
            "			<type name=\"uint32\" package=\"package0\"></type>" +
            "			<value>4096</value>" +
            "		</data>" +
            "		<data name=\"transaction_count\">" +
            "			<type name=\"uint32\" package=\"package0\"></type>" +
            "			<value>0</value>" +
            "		</data>" +
            "		<data name=\"group_id\">" +
            "			<type name=\"uint32\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "		<data name=\"duration\">" +
            "			<type name=\"uint64\" package=\"package0\"></type>" +
            "			<value>191053000</value>" +
            "		</data>" +
            "		<data name=\"client_pid\">" +
            "			<type name=\"uint32\" package=\"package0\"></type>" +
            "			<value>4680</value>" +
            "		</data>" +
            "		<data name=\"options\">" +
            "			<type name=\"binary_data\" package=\"package0\"></type>" +
            "			<value>2000002838f4010000000000</value>" +
            "		</data>" +
            "		<data name=\"options_text\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value>" +
            "				<![CDATA[-- network protocol: LPC" +
            "set quoted_identifier on" +
            "set arithabort off" +
            "set numeric_roundabort off" +
            "set ansi_warnings on" +
            "set ansi_padding on" +
            "set ansi_nulls on" +
            "set concat_null_yields_null on" +
            "set cursor_close_on_commit off" +
            "set implicit_transactions off" +
            "set language us_english" +
            "set dateformat mdy" +
            "set datefirst 7" +
            "set transaction isolation level read committed" +
            "]]>" +
            "			</value>" +
            "		</data>" +
            "		<data name=\"started_event_session_name\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[Profiler]]></value>" +
            "		</data>" +
            "		<data name=\"database_name\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[]]></value>" +
            "		</data>" +
            "		<data name=\"client_app_name\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[Microsoft SQL Server Management Studio]]></value>" +
            "		</data>" +
            "		<data name=\"client_hostname\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[KARLBURTRAMC189]]></value>" +
            "		</data>" +
            "		<data name=\"nt_domain\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[]]></value>" +
            "		</data>" +
            "		<data name=\"nt_user\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[]]></value>" +
            "		</data>" +
            "		<data name=\"session_nt_domain\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[]]></value>" +
            "		</data>" +
            "		<data name=\"session_nt_user\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[]]></value>" +
            "		</data>" +
            "		<data name=\"server_principal_name\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[sa]]></value>" +
            "		</data>" +
            "		<data name=\"server_principal_sid\">" +
            "			<type name=\"binary_data\" package=\"package0\"></type>" +
            "			<value>01</value>" +
            "		</data>" +
            "		<data name=\"session_server_principal_name\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[sa]]></value>" +
            "		</data>" +
            "		<data name=\"session_server_principal_sid\">" +
            "			<type name=\"binary_data\" package=\"package0\"></type>" +
            "			<value>01</value>" +
            "		</data>" +
            "		<action name=\"session_id\" package=\"sqlserver\">" +
            "			<type name=\"uint16\" package=\"package0\"></type>" +
            "			<value>56</value>" +
            "		</action>" +
            "		<action name=\"server_principal_name\" package=\"sqlserver\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[sa]]></value>" +
            "		</action>" +
            "		<action name=\"nt_username\" package=\"sqlserver\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[]]></value>" +
            "		</action>" +
            "		<action name=\"client_pid\" package=\"sqlserver\">" +
            "			<type name=\"uint32\" package=\"package0\"></type>" +
            "			<value>930958063</value>" +
            "		</action>" +
            "		<action name=\"client_app_name\" package=\"sqlserver\">" +
            "			<type name=\"unicode_string\" package=\"package0\"></type>" +
            "			<value><![CDATA[Core .Net SqlClient Data Provider]]></value>" +
            "		</action>" +
            "		<action name=\"attach_activity_id_xfer\" package=\"package0\">" +
            "			<type name=\"activity_id_xfer\" package=\"package0\"></type>" +
            "			<value>A2873402-C433-4D1F-94C4-9CA99749453E-0</value>" +
            "		</action>" +
            "		<action name=\"attach_activity_id\" package=\"package0\">" +
            "			<type name=\"activity_id\" package=\"package0\"></type>" +
            "			<value>770C3538-EC3F-4A27-86A9-31A2FC777DBC-1</value>" +
            "		</action>" +
            "	</event>" +
            "</RingBufferTarget>";



        public SessionId Id { get { return new SessionId("testsession_51"); } }

        public void Start(){}

        public void Stop(){}
        public string GetTargetXml()
        {
            return testXEventXml;
        }
    }

    public class TestXEventSession1 : IXEventSession
    {
        private const string testXEventXml_1 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "	</event>" +
            "</RingBufferTarget>";

        private const string testXEventXml_2 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "       </data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-10-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "       </data>" +
            "	</event>" +
            "</RingBufferTarget>";

        private const string testXEventXml_3 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "       </data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-10-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "       </data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-11-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>3</value>" +
            "       </data>" +
            "	</event>" +
            "</RingBufferTarget>";

        public SessionId Id { get { return new SessionId("testsession_1"); } }

        public void Start(){}

        public void Stop(){}

        private int pollCount = 0;
        private string[] poll_returns = { testXEventXml_1, testXEventXml_2, testXEventXml_3 };
        public string GetTargetXml()
        {
            string res = poll_returns[pollCount];
            pollCount++;
            pollCount = pollCount > 2 ? 0 : pollCount;
            return res;
        }
    }

        public class TestXEventSession2 : IXEventSession
    {
        private const string testXEventXml_1 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "       </data>" +
            "	</event>" +
            "</RingBufferTarget>";

        private const string testXEventXml_2 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "       </data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-10-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "       </data>" +
            "	</event>" +
            "</RingBufferTarget>";

        private const string testXEventXml_3 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "       </data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-10-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "       </data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-11-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "       <data name=\"event_sequence\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>3</value>" +
            "       </data>" +
            "	</event>" +
            "</RingBufferTarget>";

        public SessionId Id { get { return new SessionId("testsession_2"); } }

        public void Start(){}

        public void Stop(){}

        private int pollCount = 0;
        private string[] poll_returns = { testXEventXml_1, testXEventXml_2, testXEventXml_3 };
        public string GetTargetXml()
        {
            string res = poll_returns[pollCount];
            pollCount++;
            pollCount = pollCount > 2 ? 0 : pollCount;
            return res;
        }
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

        public IXEventSession OpenLiveStreamSession(string sessionName, ConnectionInfo connInfo)
        {
            // Return an observable live stream session for testing
            var events = new[]
            {
                new TestXEvent("existing_connection", DateTimeOffset.Parse("2017-09-08T07:46:53.579Z"),
                    new Dictionary<string, object> { { "session_id", "1" }, { "event_sequence", "1" } })
            };
            var fetcher = new TestLiveEventFetcher(events);
            return new LiveStreamXEventSession(
                () => fetcher,
                new SessionId($"testsession_{sessionNum}", sessionNum),
                maxReconnectAttempts: 0);
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

        public TestLiveEventFetcher(
            IEnumerable<IXEvent> events,
            bool failImmediately = false,
            Exception exceptionToThrow = null,
            TimeSpan? delayBetweenEvents = null,
            int failAfterEvents = -1)
        {
            this.events = events ?? Array.Empty<IXEvent>();
            this.failImmediately = failImmediately;
            this.failAfterEvents = failAfterEvents;
            this.exceptionToThrow = exceptionToThrow ?? new Exception("Simulated stream failure");
            this.delayBetweenEvents = delayBetweenEvents ?? TimeSpan.Zero;
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

        public IXEventSession OpenLiveStreamSession(string sessionName, ConnectionInfo connInfo)
        {
            return CreateLiveStreamSession(sessionName);
        }

        private LiveStreamXEventSession CreateLiveStreamSession(string sessionName)
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
