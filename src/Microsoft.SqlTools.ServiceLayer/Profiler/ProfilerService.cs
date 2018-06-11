//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Main class for Profiler Service functionality
    /// </summary>
    public sealed class ProfilerService : IDisposable, IXEventSessionFactory, IProfilerSessionListener
    {
        private bool disposed;

        private ConnectionService connectionService = null;

        private ProfilerSessionMonitor monitor = new ProfilerSessionMonitor();

        private static readonly Lazy<ProfilerService> instance = new Lazy<ProfilerService>(() => new ProfilerService());

        /// <summary>
        /// Construct a new ProfilerService instance with default parameters
        /// </summary>
        public ProfilerService()
        {
            this.XEventSessionFactory = this;
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static ProfilerService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// XEvent session factory.  Internal to allow mocking in unit tests.
        /// </summary>
        internal IXEventSessionFactory XEventSessionFactory { get; set; }

        /// <summary>
        /// Session monitor instance
        /// </summary>
        internal ProfilerSessionMonitor SessionMonitor
        {
            get
            {
                return this.monitor;
            }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the Profiler Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            //do I need to make a new request handler for pausing?
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(StartProfilingRequest.Type, HandleStartProfilingRequest);
            this.ServiceHost.SetRequestHandler(StopProfilingRequest.Type, HandleStopProfilingRequest);
            this.ServiceHost.SetRequestHandler(PauseProfilingRequest.Type, HandlePauseProfilingRequest);

            this.SessionMonitor.AddSessionListener(this);
        }

        /// <summary>
        /// Handle request to start a profiling session
        /// </summary>
        internal async Task HandleStartProfilingRequest(StartProfilingParams parameters, RequestContext<StartProfilingResult> requestContext)
        {
            try
            {
                var result = new StartProfilingResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    int xEventSessionID = StartSession(parameters.OwnerUri, parameters.TemplateName, connInfo);
                    result.SessionId = xEventSessionID.ToString();
                    result.Succeeded = true;
                }
                else
                {
                    result.Succeeded = false;
                    result.ErrorMessage = SR.ProfilerConnectionNotFound;
                }

                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handle request to stop a profiling session
        /// </summary>
        internal async Task HandleStopProfilingRequest(StopProfilingParams parameters, RequestContext<StopProfilingResult> requestContext)
        {
            try
            {
                ProfilerSession session;
                monitor.StopMonitoringSession(parameters.OwnerUri, out session);
                session.XEventSession.Stop();

                await requestContext.SendResult(new StopProfilingResult
                {
                    Succeeded = true
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handle request to pause a profiling session
        /// </summary>
        internal async Task HandlePauseProfilingRequest(PauseProfilingParams parameters, RequestContext<PauseProfilingResult> requestContext)
        {
            try
            {
                monitor.PauseViewer(parameters.OwnerUri);

                await requestContext.SendResult(new PauseProfilingResult
                {
                    Succeeded = true
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Starts a new profiler session or connects to an existing session
        /// for the provided connection and template info
        /// </summary>
        /// <returns>
        /// The XEvent Session ID that was started
        /// </returns>
        internal int StartSession(string ownerUri, string template, ConnectionInfo connInfo)
        {
            // create a new XEvent session and Profiler session
            var xeSession = this.XEventSessionFactory.GetOrCreateXEventSession(template, connInfo);

            // start monitoring the profiler session
            monitor.StartMonitoringSession(ownerUri, xeSession);

            return xeSession.ID;
        }

        /// <summary>
        /// Gets or creates an XEvent session with the given template per the IXEventSessionFactory contract
        /// Also starts the session if it isn't currently running
        /// </summary>
        public IXEventSession GetOrCreateXEventSession(string template, ConnectionInfo connInfo)
        {
            // TODO: Change this to handle different names based off of templates
            string sessionName = "Profiler";

            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            XEStore store = new XEStore(connection);
            Session session = store.Sessions[sessionName];

            // start the session if it isn't already running
            if (session == null)
            {
                session = CreateSession(connection, sessionName);
            }

            if (session != null && !session.IsRunning)
            {
                session.Start();
            }

            // create xevent session wrapper
            return new XEventSession()
            {
                Session = session
            };
        }

        private static Session CreateSession(SqlStoreConnection connection, string sessionName)
        {
            string createSessionSql =
                @"
                CREATE EVENT SESSION [Profiler] ON SERVER
                ADD EVENT sqlserver.attention(
                    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.database_id,sqlserver.nt_username,sqlserver.query_hash,sqlserver.server_principal_name,sqlserver.session_id)
                    WHERE ([package0].[equal_boolean]([sqlserver].[is_system],(0)))),
                ADD EVENT sqlserver.existing_connection(SET collect_options_text=(1)
                    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.nt_username,sqlserver.server_principal_name,sqlserver.session_id)),
                ADD EVENT sqlserver.login(SET collect_options_text=(1)
                    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.nt_username,sqlserver.server_principal_name,sqlserver.session_id)),
                ADD EVENT sqlserver.logout(
                    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.nt_username,sqlserver.server_principal_name,sqlserver.session_id)),
                ADD EVENT sqlserver.rpc_completed(
                    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.database_id,sqlserver.nt_username,sqlserver.query_hash,sqlserver.server_principal_name,sqlserver.session_id)
                    WHERE ([package0].[equal_boolean]([sqlserver].[is_system],(0)))),
                ADD EVENT sqlserver.sql_batch_completed(
                    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.database_id,sqlserver.nt_username,sqlserver.query_hash,sqlserver.server_principal_name,sqlserver.session_id)
                    WHERE ([package0].[equal_boolean]([sqlserver].[is_system],(0)))),
                ADD EVENT sqlserver.sql_batch_starting(
                    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.database_id,sqlserver.nt_username,sqlserver.query_hash,sqlserver.server_principal_name,sqlserver.session_id)
                    WHERE ([package0].[equal_boolean]([sqlserver].[is_system],(0))))
                ADD TARGET package0.ring_buffer(SET max_events_limit=(1000),max_memory=(51200))
                WITH (MAX_MEMORY=8192 KB,EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,MAX_DISPATCH_LATENCY=5 SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=PER_CPU,TRACK_CAUSALITY=ON,STARTUP_STATE=OFF)";

            connection.ServerConnection.ExecuteNonQuery(createSessionSql);

            XEStore store = new XEStore(connection);
            return store.Sessions[sessionName];
        }

        /// <summary>
        /// Callback when profiler events are available
        /// </summary>
        public void EventsAvailable(string sessionId, List<ProfilerEvent> events)
        {
            // pass the profiler events on to the client
            this.ServiceHost.SendEvent(
                ProfilerEventsAvailableNotification.Type,
                new ProfilerEventsAvailableParams()
                {
                    OwnerUri = sessionId,
                    Events = events
                });
        }

        /// <summary>
        /// Disposes the Profiler Service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}
