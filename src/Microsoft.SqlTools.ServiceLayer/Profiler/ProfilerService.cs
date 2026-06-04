//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
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
        internal IRpcServiceHost ServiceHost
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
            this.ServiceHost.RegisterRequestHandler(CreateXEventSessionRequest.Type, HandleCreateXEventSessionRequest);
            this.ServiceHost.RegisterRequestHandler(StartProfilingRequest.Type, HandleStartProfilingRequest);
            this.ServiceHost.RegisterRequestHandler(StopProfilingRequest.Type, HandleStopProfilingRequest);
            this.ServiceHost.RegisterRequestHandler(PauseProfilingRequest.Type, HandlePauseProfilingRequest);
            this.ServiceHost.RegisterRequestHandler(GetXEventSessionsRequest.Type, HandleGetXEventSessionsRequest);
            this.ServiceHost.RegisterRequestHandler(DisconnectSessionRequest.Type, HandleDisconnectSessionRequest);

            this.SessionMonitor.AddSessionListener(this);
        }

        /// <summary>
        /// Handle request to start a profiling session
        /// </summary>
        internal async Task<CreateXEventSessionResult> HandleCreateXEventSessionRequest(CreateXEventSessionParams parameters)
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

                // create a new Extended Events session if it doesn't exist
                xeSession ??= this.XEventSessionFactory.CreateXEventSession(parameters.Template.CreateStatement, parameters.SessionName, connInfo);

                // start monitoring the event session
                monitor.StartMonitoringSession(parameters.OwnerUri, xeSession);

                SessionCreatedNotification(parameters.OwnerUri, parameters.SessionName, parameters.Template.Name);
                var result = new CreateXEventSessionResult();
                return result;
            }
        }

        /// <summary>
        /// Handle request to start a profiling session
        /// </summary>
        internal async Task<StartProfilingResult> HandleStartProfilingRequest(StartProfilingParams parameters)
        {
            if (parameters.SessionType == ProfilingSessionType.LocalFile)
            {
                return await StartLocalFileSession(parameters);
            }
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                parameters.OwnerUri,
                out connInfo);
            if (connInfo != null)
            {
                // Get the Extended Events session
                var xeSession = this.XEventSessionFactory.GetXEventSession(parameters.SessionName, connInfo);

                // start monitoring the event session
                monitor.StartMonitoringSession(parameters.OwnerUri, xeSession);

                var result = new StartProfilingResult() { CanPause = true, UniqueSessionId = xeSession.Id.ToString() };
                return result;
            }
            else
            {
                throw new ProfilerException(SR.ProfilerConnectionNotFound);
            }
        }

        private async Task<StartProfilingResult> StartLocalFileSession(StartProfilingParams parameters)
        {
            var xeSession = XEventSessionFactory.OpenLocalFileSession(parameters.SessionName);
            monitor.StartMonitoringSession(parameters.OwnerUri, xeSession);
            xeSession.Start();
            var result = new StartProfilingResult() { UniqueSessionId = xeSession.Id.ToString(), CanPause = false };
            return result;
        }

        public IXEventSession OpenLocalFileSession(string filePath)
        {
            return new LocalFileXEventSession(() => initIXEventFetcher(filePath), new SessionId(filePath));
        }

        private IXEventFetcher initIXEventFetcher(string filePath)
        {
            return new XEFileEventStreamer(filePath);
        }

        /// <summary>
        /// Handle request to stop a profiling session
        /// </summary>
        internal async Task<StopProfilingResult> HandleStopProfilingRequest(StopProfilingParams parameters)
        {
            monitor.StopMonitoringSession(parameters.OwnerUri, out ProfilerSession session);

            if (session != null)
            {
                // Occasionally we might see the InvalidOperationException due to a read is 
                // in progress, add the following retry logic will solve the problem.
                int remainingAttempts = ProfilerConstants.StopSessionMaxRetryAttempts;
                while (true)
                {
                    try
                    {
                        session.XEventSession.Stop();
                        session.Dispose();
                        return new StopProfilingResult { };
                    }
                    catch (InvalidOperationException)
                    {
                        remainingAttempts--;
                        if (remainingAttempts == 0)
                        {
                            throw;
                        }
                        await Task.Delay(ProfilerConstants.StopSessionRetryDelay);
                    }
                }
            }
            else
            {
                throw new ProfilerException(SR.SessionNotFound);
            }
        }

        /// <summary>
        /// Handle request to pause or resume a profiling session
        /// calling on a running session will pause the profiling session
        /// and calling on a paused session will resume the profiling session
        /// </summary>
        internal async Task<PauseProfilingResult> HandlePauseProfilingRequest(PauseProfilingParams parameters)
        {
            if (parameters == null || string.IsNullOrEmpty(parameters.OwnerUri))
            {
                throw RpcErrorException.Create(new ProfilerException(SR.SessionNotFound));
            }

            if (!monitor.PauseViewer(parameters.OwnerUri, out bool isPaused))
            {
                throw RpcErrorException.Create(new ProfilerException(SR.SessionNotFound));
            }

            return new PauseProfilingResult { IsPaused = isPaused };
        }

        /// <summary>
        /// Handle request to pause a profiling session
        /// </summary>
        internal async Task<GetXEventSessionsResult> HandleGetXEventSessionsRequest(GetXEventSessionsParams parameters)
        {
            var result = new GetXEventSessionsResult();
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                parameters.OwnerUri,
                out connInfo);
            if (connInfo == null)
            {
                throw RpcErrorException.Create(new ProfilerException(SR.ProfilerConnectionNotFound));
            }
            else
            {
                List<string> sessions = GetXEventSessionList(connInfo);
                result.Sessions = sessions;
                return result;
            }
        }

        /// <summary>
        /// Handle request to disconnect a session
        /// </summary>
        internal Task<DisconnectSessionResult> HandleDisconnectSessionRequest(DisconnectSessionParams parameters)
        {
            monitor.StopMonitoringSession(parameters.OwnerUri, out _);
            return Task.FromResult(new DisconnectSessionResult());
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
        /// Gets an Extended Events session with the given name per the IXEventSessionFactory contract.
        /// Uses XELite's XELiveEventStreamer for push-based event delivery.
        /// Also starts the session if it isn't currently running.
        /// </summary>
        public IXEventSession GetXEventSession(string sessionName, ConnectionInfo connInfo)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            BaseXEStore store = CreateXEventStore(connInfo, connection);
            store.Sessions.Refresh();
            Session session = store.Sessions[sessionName] ?? throw new ProfilerException(SR.SessionNotFound);

            // Ensure the session is not running before starting it
            if (!session.IsRunning)
            {
                session.Start();
            }

            // Build connection string for XELite
            var connectionString = BuildXELiteConnectionString(sqlConnection, connInfo);

            // Create the live streaming session using XELite
            var liveSession = new LiveStreamXEventSession(
                connectionString,
                sessionName,
                new SessionId(session.ID.ToString()));

            // Set the SMO session for target XML retrieval
            liveSession.Session = session;
            liveSession.SqlConnection = sqlConnection;

            return liveSession;
        }

        /// <summary>
        /// Creates and starts an Extended Events session with the given name and create statement per the IXEventSessionFactory contract
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
            session = store.Sessions[sessionName] ?? throw new ProfilerException(SR.SessionNotFound);
            if (!session.IsRunning)
            {
                session.Start();
            }

            // Build connection string for XELite
            var connectionString = BuildXELiteConnectionString(sqlConnection, connInfo);

            // Create the live streaming session using XELite
            var liveSession = new LiveStreamXEventSession(
                connectionString,
                sessionName,
                new SessionId(session.ID.ToString()));

            // Set the session for session management
            liveSession.Session = session;
            liveSession.SqlConnection = sqlConnection;

            return liveSession;
        }

        /// <summary>
        /// Builds the connection string handed to XELite's <c>XELiveEventStreamer</c>.
        /// </summary>
        /// <remarks>
        /// XELite creates its own internal <see cref="SqlConnection"/> from the connection
        /// string and cannot accept a pre-acquired access token or an
        /// <see cref="SqlConnection.AccessTokenCallback"/>.
        ///
        /// To bridge the gap for Microsoft Entra MFA connections, this method rewrites the
        /// connection string to use <see cref="SqlAuthenticationMethod.ActiveDirectoryInteractive"/>
        /// with the connection's account id placed in <c>User ID</c>, and registers an
        /// <see cref="XEventAuthenticationProvider"/> token fetcher keyed on
        /// <c>(accountId, tenantId)</c>.
        /// </remarks>
        internal static string BuildXELiteConnectionString(SqlConnection sqlConnection, ConnectionInfo connInfo)
        {
            var connectionString = sqlConnection.ConnectionString;

            if (connInfo?.AzureTokenFetcher == null
                || connInfo.ConnectionDetails?.AuthenticationType != Microsoft.SqlTools.Utility.SqlConstants.AzureMFA
                || string.IsNullOrEmpty(connInfo.ConnectionDetails.AccountId))
            {
                return connectionString;
            }

            XEventAuthenticationProvider.Register(
                connInfo.ConnectionDetails.AccountId,
                connInfo.ConnectionDetails.TenantId,
                connInfo.AzureTokenFetcher);

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive,
                UserID = connInfo.ConnectionDetails.AccountId,
            };

            return builder.ConnectionString;
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
        /// Callback when the Extended Events session is closed unexpectedly
        /// </summary>
        public void SessionStopped(string viewerId, SessionId sessionId, string errorMessage)
        {
            // notify the client that their event session closed
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
        /// Callback when a new event session is created
        /// </summary>
        public void SessionCreatedNotification(string viewerId, string sessionName, string templateName)
        {
            // notify the client that the event session was created
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
