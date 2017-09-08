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
    public sealed class ProfilerService : IDisposable, IXEventSessionFactory
    {
        private bool disposed;

        private ConnectionService connectionService = null;

        private ProfilerSessionMonitor monitor = new ProfilerSessionMonitor();

        private static readonly Lazy<ProfilerService> instance = new Lazy<ProfilerService>(() => new ProfilerService());

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

        internal IXEventSessionFactory XEventSessionFactory { get; set; }

        public ProfilerService()
        {
            this.XEventSessionFactory = this;
        }

        public IXEventSession CreateXEventSession(ConnectionInfo connInfo)
        {
            SqlConnectionStringBuilder connectionBuilder;          
            connectionBuilder = new SqlConnectionStringBuilder
            {
                IntegratedSecurity = false,
                ["Data Source"] = "localhost",
                ["User Id"] = "sa",
                ["Password"] = "Yukon900",
                ["Initial Catalog"] = "master"
            };

            SqlConnection sqlConnection = new SqlConnection(connectionBuilder.ToString());
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            XEStore store = new XEStore(connection);
            Session session = store.Sessions["Profiler"];

            try
            {
                // start the session if it isn't already running
                if (!session.IsRunning)
                {
                    session.Start();
                }
            }
            catch { }

            // create xevent session wrapper
            return new XEventSession()
            {
                Session = session
            };
        }

        /// <summary>
        /// Initializes the Profiler Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(StartProfilingRequest.Type, HandleStartProfilingRequest);
            serviceHost.SetRequestHandler(StopProfilingRequest.Type, HandleStopProfilingRequest);
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
                await requestContext.SendResult(new StopProfilingResult());
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            } 
        }

        public ProfilerSession StartSession(ConnectionInfo connInfo)
        {
            var xeSession = this.XEventSessionFactory.CreateXEventSession(connInfo);
            var profilerSession = new ProfilerSession()
            {
                SessionId = Guid.NewGuid().ToString(),
                XEventSession = xeSession
            };

            monitor.StartMonitoringSession(profilerSession);

            return profilerSession;
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
