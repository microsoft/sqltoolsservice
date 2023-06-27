//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.Management.XEventDbScoped;
using Microsoft.SqlServer.XEvent.XELite;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

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
                connectionService ??= ConnectionService.Instance;
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
            this.ServiceHost.SetRequestHandler(CreateXEventSessionRequest.Type, HandleCreateXEventSessionRequest, true);
            this.ServiceHost.SetRequestHandler(StartProfilingRequest.Type, HandleStartProfilingRequest, true);
            this.ServiceHost.SetRequestHandler(StopProfilingRequest.Type, HandleStopProfilingRequest, true);
            this.ServiceHost.SetRequestHandler(PauseProfilingRequest.Type, HandlePauseProfilingRequest, true);
            this.ServiceHost.SetRequestHandler(GetXEventSessionsRequest.Type, HandleGetXEventSessionsRequest, true);
            this.ServiceHost.SetRequestHandler(DisconnectSessionRequest.Type, HandleDisconnectSessionRequest, true);

            this.SessionMonitor.AddSessionListener(this);
        }

        /// <summary>
        /// Handle request to start a profiling session
        /// </summary>
        internal async Task HandleCreateXEventSessionRequest(CreateXEventSessionParams parameters, RequestContext<CreateXEventSessionResult> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                parameters.OwnerUri,
                out connInfo);
            if (connInfo == null)
            {
                throw new ProfilerException(SR.ProfilerConnectionNotFound);
            }
            else if (parameters.SessionName == null)
            {
                throw new ArgumentNullException("SessionName");
            }
            else if (parameters.Template == null)
            {
                throw new ArgumentNullException("Template");
            }
            else
            {
                IXEventSession xeSession = null;

                // first check whether the session with the given name already exists.
                // if so skip the creation part. An exception will be thrown if no session with given name can be found,
                // and it can be ignored.
                try
                {
                    xeSession = this.XEventSessionFactory.GetXEventSession(parameters.SessionName, connInfo);
                }
                catch { }

                // create a new XEvent session and Profiler session, if it doesn't exist
                xeSession ??= this.XEventSessionFactory.CreateXEventSession(parameters.Template.CreateStatement, parameters.SessionName, connInfo);

                // start monitoring the profiler session
                monitor.StartMonitoringSession(parameters.OwnerUri, xeSession);

                var result = new CreateXEventSessionResult();
                await requestContext.SendResult(result);

                SessionCreatedNotification(parameters.OwnerUri, parameters.SessionName, parameters.Template.Name);
            }
        }

        /// <summary>
        /// Handle request to start a profiling session
        /// </summary>
        internal async Task HandleStartProfilingRequest(StartProfilingParams parameters, RequestContext<StartProfilingResult> requestContext)
        {
            if (parameters.SessionType == ProfilingSessionType.LocalFile)
            {
                await StartLocalFileSession(parameters, requestContext);
                return;
            }
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                parameters.OwnerUri,
                out connInfo);
            if (connInfo != null)
            {
                // create a new XEvent session and Profiler session
                var xeSession = this.XEventSessionFactory.GetXEventSession(parameters.SessionName, connInfo);

                // start monitoring the profiler session
                monitor.StartMonitoringSession(parameters.OwnerUri, xeSession);

                var result = new StartProfilingResult() { CanPause = true, UniqueSessionId = xeSession.Id.ToString() };
                await requestContext.SendResult(result);
            }
            else
            {
                throw new ProfilerException(SR.ProfilerConnectionNotFound);
            }
        }

        private async Task StartLocalFileSession(StartProfilingParams parameters, RequestContext<StartProfilingResult> requestContext)
        {
            var xeSession = XEventSessionFactory.OpenLocalFileSession(parameters.SessionName);
            monitor.StartMonitoringSession(parameters.OwnerUri, xeSession);
            xeSession.Start();
            var result = new StartProfilingResult() { UniqueSessionId = xeSession.Id.ToString(), CanPause = false };
            await requestContext.SendResult(result);
        }

        public IXEventSession OpenLocalFileSession(string filePath)
        {
            return new ObservableXEventSession(() => initIXEventFetcher(filePath), new SessionId(filePath));
        }

        public IXEventFetcher initIXEventFetcher(string filePath)
        {
            return new XEFileEventStreamer(filePath);
        }

        /// <summary>
        /// Handle request to stop a profiling session
        /// </summary>
        internal async Task HandleStopProfilingRequest(StopProfilingParams parameters, RequestContext<StopProfilingResult> requestContext)
        {
            monitor.StopMonitoringSession(parameters.OwnerUri, out ProfilerSession session);

            if (session != null)
            {
                // Occasionally we might see the InvalidOperationException due to a read is 
                // in progress, add the following retry logic will solve the problem.
                int remainingAttempts = 3;
                while (true)
                {
                    try
                    {
                        session.XEventSession.Stop();
                        session.Dispose();
                        await requestContext.SendResult(new StopProfilingResult { });
                        break;
                    }
                    catch (InvalidOperationException)
                    {
                        remainingAttempts--;
                        if (remainingAttempts == 0)
                        {
                            throw;
                        }
                        await Task.Delay(500);
                    }
                }
            }
            else
            {
                throw new ProfilerException(SR.SessionNotFound);
            }
        }

        /// <summary>
        /// Handle request to pause a profiling session
        /// </summary>
        internal async Task HandlePauseProfilingRequest(PauseProfilingParams parameters, RequestContext<PauseProfilingResult> requestContext)
        {
            monitor.PauseViewer(parameters.OwnerUri);

            await requestContext.SendResult(new PauseProfilingResult { });
        }

        /// <summary>
        /// Handle request to pause a profiling session
        /// </summary>
        internal async Task HandleGetXEventSessionsRequest(GetXEventSessionsParams parameters, RequestContext<GetXEventSessionsResult> requestContext)
        {
            var result = new GetXEventSessionsResult();
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                parameters.OwnerUri,
                out connInfo);
            if (connInfo == null)
            {
                await requestContext.SendError(new ProfilerException(SR.ProfilerConnectionNotFound));
            }
            else
            {
                List<string> sessions = GetXEventSessionList(connInfo);
                result.Sessions = sessions;
                await requestContext.SendResult(result);
            }
        }

        /// <summary>
        /// Handle request to disconnect a session
        /// </summary>
        internal Task HandleDisconnectSessionRequest(DisconnectSessionParams parameters, RequestContext<DisconnectSessionResult> requestContext)
        {
            monitor.StopMonitoringSession(parameters.OwnerUri, out _);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a list of all running XEvent Sessions
        /// </summary>
        /// <returns>
        /// A list of the names of all running XEvent sessions
        /// </returns>
        internal List<string> GetXEventSessionList(ConnectionInfo connInfo)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            BaseXEStore store = CreateXEventStore(connInfo, connection);

            // get session names from the session list
            List<string> results = store.Sessions.Aggregate(new List<string>(), (result, next) =>
            {
                result.Add(next.Name);
                return result;
            });

            return results;
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
        /// Gets an XEvent session with the given name per the IXEventSessionFactory contract
        /// Also starts the session if it isn't currently running
        /// </summary>
        public IXEventSession GetXEventSession(string sessionName, ConnectionInfo connInfo)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            BaseXEStore store = CreateXEventStore(connInfo, connection);
            Session session = store.Sessions[sessionName] ?? throw new Exception(SR.SessionNotFound);

            // start the session if it isn't already running

            session = session ?? throw new ProfilerException(SR.SessionNotFound);

            var xeventSession = new XEventSession()
            {
                Session = session
            };

            if (!session.IsRunning)
            {
                xeventSession.Start();
            }
            return xeventSession;
        }

        /// <summary>
        /// Creates and starts an XEvent session with the given name and create statement per the IXEventSessionFactory contract
        /// </summary>
        public IXEventSession CreateXEventSession(string createStatement, string sessionName, ConnectionInfo connInfo)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            BaseXEStore store = CreateXEventStore(connInfo, connection);
            Session session = store.Sessions[sessionName];

            // session shouldn't already exist
            if (session != null)
            {
                throw new ProfilerException(SR.SessionAlreadyExists(sessionName));
            }

            var statement = createStatement.Replace("{sessionName}", sessionName);
            connection.ServerConnection.ExecuteNonQuery(statement);
            store.Refresh();
            session = store.Sessions[sessionName];
            if (session == null)
            {
                throw new ProfilerException(SR.SessionNotFound);
            }
            if (!session.IsRunning)
            {
                session.Start();
            }

            // create xevent session wrapper
            return new XEventSession()
            {
                Session = store.Sessions[sessionName]
            };
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
        public void SessionStopped(string viewerId, SessionId sessionId, string errorMessage)
        {
            // notify the client that their session closed
            this.ServiceHost.SendEvent(
                ProfilerSessionStoppedNotification.Type,
                new ProfilerSessionStoppedParams()
                {
                    OwnerUri = viewerId,
                    SessionId = sessionId.NumericId,
                    ErrorMessage = errorMessage
                });
        }

        /// <summary>
        /// Callback when a new session is created
        /// </summary>
        public void SessionCreatedNotification(string viewerId, string sessionName, string templateName)
        {
            // pass the profiler events on to the client
            this.ServiceHost.SendEvent(
                ProfilerSessionCreatedNotification.Type,
                new ProfilerSessionCreatedParams()
                {
                    OwnerUri = viewerId,
                    SessionName = sessionName,
                    TemplateName = templateName
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
