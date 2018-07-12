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
using Microsoft.SqlServer.Management.XEventDbScoped;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
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
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(CreateProfilerSessionRequest.Type, HandleCreateProfilerSessionRequest);
            this.ServiceHost.SetRequestHandler(StartProfilingRequest.Type, HandleStartProfilingRequest);
            this.ServiceHost.SetRequestHandler(StopProfilingRequest.Type, HandleStopProfilingRequest);
            this.ServiceHost.SetRequestHandler(PauseProfilingRequest.Type, HandlePauseProfilingRequest);
            this.ServiceHost.SetRequestHandler(ListAvailableSessionsRequest.Type, HandleListAvailableSessionsRequest);

            this.SessionMonitor.AddSessionListener(this);
        }

        /// <summary>
        /// Handle request to start a profiling session
        /// </summary>
        internal async Task HandleCreateProfilerSessionRequest(CreateProfilerSessionParams parameters, RequestContext<CreateProfilerSessionResult> requestContext)
        {
            try
            {
                var result = new CreateProfilerSessionResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    // TODO: Error messages here
                    var session = this.XEventSessionFactory.CreateXEventSession(parameters.CreateStatement, parameters.SessionName, connInfo);
                    this.monitor.StartMonitoringSession(parameters.OwnerUri, session);
                    result.SessionId = session.Id.ToString();
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
                    var session = this.XEventSessionFactory.GetXEventSession(parameters.XEventSessionName, connInfo);
                    if (session != null)
                    {
                        this.monitor.StartMonitoringSession(parameters.OwnerUri, session);
                        result.SessionId = session.Id.ToString();
                        result.Succeeded = true;
                    }
                    else
                    {
                        // TODO: Localize string
                        result.Succeeded = false;
                        result.ErrorMessage = "Invalid session name";
                    }         
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

                await requestContext.SendResult(new StopProfilingResult{});
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

                await requestContext.SendResult(new PauseProfilingResult{});
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handle request for a list of all available XEvent Sessions
        /// </summary>
        internal async Task HandleListAvailableSessionsRequest(ListAvailableSessionsParams parameters, RequestContext<ListAvailableSessionsResult> requestContext)
        {
            try
            {
                var result = new ListAvailableSessionsResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    List<string> sessions = GetXEventSessionList(parameters.OwnerUri, connInfo);
                    result.AvailableSessions = sessions;
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
        /// Gets or creates an XEvent session with the given template per the IXEventSessionFactory contract
        /// Also starts the session if it isn't currently running
        /// </summary>
        public IXEventSession CreateXEventSession(string createStatement, string sessionName, ConnectionInfo connInfo)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            BaseXEStore store = CreateXEventStore(connInfo, connection);
            Session session = store.Sessions[sessionName];

            // make sure the session doesn't already exist
            if (session != null)
            {
                // TODO: Localize string
                throw new ArgumentException("Session cannot be created because another session with the same name already exists.");
            }

            // replace template default with actual session name
            createStatement = createStatement.Replace("PROFILER_SESSION_NAME", sessionName);

            // create new session
            connection.ServerConnection.ExecuteNonQuery(createStatement);
            store.Refresh();

            // create xevent session wrapper
            return new XEventSession()
            {
                Session = store.Sessions[sessionName]
            };
        }

        /// <summary>
        /// Gets or creates an XEvent session with the given template per the IXEventSessionFactory contract
        /// Also starts the session if it isn't currently running
        /// </summary>
        public IXEventSession GetXEventSession(string sessionName, ConnectionInfo connInfo)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            BaseXEStore store = CreateXEventStore(connInfo, connection);
            Session session = store.Sessions[sessionName];

            // make sure the session exists
            if (session != null)
            {
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
            else
            {
                return null;
            }
        }

        private static BaseXEStore CreateXEventStore(ConnectionInfo connInfo, SqlStoreConnection connection)
        {   
            BaseXEStore store = null;
            if (connInfo.IsCloud)
            {
                if (DatabaseUtils.IsSystemDatabaseConnection(connInfo.ConnectionDetails.DatabaseName))
                {
                    throw new NotSupportedException(SR.AzureSystemDbProfilingError);
                }
                store = new DatabaseXEStore(connection, connInfo.ConnectionDetails.DatabaseName);
            }
            else
            {
                store = new XEStore(connection);
            }
            return store;
        }

        /// <summary>
        /// Lists all available XEvent sessions
        /// </summary>
        /// <returns>
        /// a list of all available XEvent sessions
        /// </returns>
        internal List<string> GetXEventSessionList(string ownerUri, ConnectionInfo connInfo)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            BaseXEStore store = CreateXEventStore(connInfo, connection);

            // get session names out of session list
            List<string> results = store.Sessions.Aggregate(new List<string>(), (result, next) => {
                result.Add(next.Name);
                return result;
            } );

            return results;
        }

        /// <summary>
        /// Callback when profiler events are available
        /// </summary>
        public void EventsAvailable(string sessionId, List<ProfilerEvent> events, bool eventsLost)
        {
            // pass the profiler events on to the client
            this.ServiceHost.SendEvent(
                ProfilerEventsAvailableNotification.Type,
                new ProfilerEventsAvailableParams()
                {
                    OwnerUri = sessionId,
                    Events = events,
                    EventsLost = eventsLost
                });
        }

        /// <summary>
        /// Callback when the XEvent session is closed unexpectedly
        /// </summary>
        public void SessionStopped(string viewerId, int sessionId)
        {
            // notify the client that their session closed
            this.ServiceHost.SendEvent(
                ProfilerSessionStoppedNotification.Type,
                new ProfilerSessionStoppedParams()
                {
                    OwnerUri = viewerId,
                    SessionId = sessionId
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
