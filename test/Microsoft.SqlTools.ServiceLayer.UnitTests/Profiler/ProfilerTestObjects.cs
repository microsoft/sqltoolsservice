//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    public static class ProfilerTestObjects
    {
        public static List<ProfilerEvent> TestProfilerEvents
        {
            get
            {
                return new List<ProfilerEvent>
                {
                    new ProfilerEvent("event1", "1/1/2017"),
                    new ProfilerEvent("event2", "1/2/2017"),
                    new ProfilerEvent("event3", "1/3/2017")
                };
            }
        }
    }

    public class TestSessionListener : IProfilerSessionListener
    {
        public string PreviousSessionId { get; set; }

        public List<ProfilerEvent> PreviousEvents { get; set; }

        public void EventsAvailable(string sessionId, List<ProfilerEvent> events)
        {
            this.PreviousSessionId = sessionId;
            this.PreviousEvents = events;
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



        public int Id { get { return 51; } }

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
            "	</event>" +
            "</RingBufferTarget>";

        private const string testXEventXml_2 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-10-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "	</event>" +
            "</RingBufferTarget>";

        private const string testXEventXml_3 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-10-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-11-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>1</value>" +
            "		</data>" +
            "	</event>" +
            "</RingBufferTarget>";

        public int Id { get { return 1; } }

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
            "	</event>" +
            "</RingBufferTarget>";

        private const string testXEventXml_2 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-10-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "	</event>" +
            "</RingBufferTarget>";

        private const string testXEventXml_3 =
            "<RingBufferTarget truncated=\"0\" processingTime=\"3\" totalEventsProcessed=\"1\" eventCount=\"1\" droppedCount=\"0\" memoryUsed=\"47996\">" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-09-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-10-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "	</event>" +
            "	<event name=\"existing_connection\" package=\"sqlserver\" timestamp=\"2017-11-08T07:46:53.579Z\">" +
            "		<data name=\"session_id\">" +
            "			<type name=\"int16\" package=\"package0\"></type>" +
            "			<value>2</value>" +
            "		</data>" +
            "	</event>" +
            "</RingBufferTarget>";

        public int Id { get { return 2; } }

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
        public IXEventSession GetOrCreateXEventSession(string template, ConnectionInfo connInfo)
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
    }
}
