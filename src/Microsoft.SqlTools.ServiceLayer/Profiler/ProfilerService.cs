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
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(StartProfilingRequest.Type, HandleStartProfilingRequest);
            this.ServiceHost.SetRequestHandler(StopProfilingRequest.Type, HandleStopProfilingRequest);

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
                    ProfilerSession session = StartSession(connInfo);
                    result.SessionId = session.SessionId;
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
                monitor.StopMonitoringSession(parameters.SessionId);
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
        /// Starts a new profiler session for the provided connection
        /// </summary>
        internal ProfilerSession StartSession(ConnectionInfo connInfo)
        {
            // create a new XEvent session and Profiler session
            var xeSession = this.XEventSessionFactory.CreateXEventSession(connInfo);
            var profilerSession = new ProfilerSession()
            {
                SessionId = Guid.NewGuid().ToString(),
                XEventSession = xeSession
            };

            // start monitoring the profiler session
            monitor.StartMonitoringSession(profilerSession);

            return profilerSession;
        }

        /// <summary>
        /// Create a new XEvent sessions per the IXEventSessionFactory contract
        /// </summary>
        public IXEventSession CreateXEventSession(ConnectionInfo connInfo)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            Session session = ProfilerService.GetOrCreateSession(connection, "Profiler");

            // create xevent session wrapper
            return new XEventSession()
            {
                Session = session
            };
        }

        /// <summary>
        /// Gets an existing XEvent session or creates one if no matching session exists.
        /// Also starts the session if it isn't currently running
        /// </summary>
        private static Session GetOrCreateSession(SqlStoreConnection connection, string sessionName)
        {
            XEStore store = new XEStore(connection);
            Session session = store.Sessions["Profiler"];
            // start the session if it isn't already running
            if (session != null && !session.IsRunning)
            {
                session.Start();
            }
            return session;
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
                    SessionId = sessionId,
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
