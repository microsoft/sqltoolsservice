//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlServer.Management.Assessment;
using  Microsoft.SqlServer.Management.Assessment.Logics;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.SqlServer.Management.Assessment.Configuration;
using Microsoft.SqlTools.ServiceLayer.Migration.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration
{
    public class Logic : ILogicsProvider {
        public Task<List<IAssessmentResult>> GetAssessmentResults(
            IAssessmentRequest request, 
            DbConnection connection, 
            EngineConfig configuration)
        {
            return null;
        }
    }

    /// <summary>
    /// Main class for Migration Service functionality
    /// </summary>
    public sealed class MigrationService : IDisposable
    {        
        private bool disposed;

        private ConnectionService connectionService = null;
     
        private static readonly Lazy<MigrationService> instance = new Lazy<MigrationService>(() => new MigrationService());

        /// <summary>
        /// Construct a new MigrationService instance with default parameters
        /// </summary>
        public MigrationService()
        {
            
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static MigrationService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Gets the <see cref="Engine"/> used to run assessment operations.
        /// </summary>
        internal Engine Engine { get; } = new Engine();

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
        /// Initializes the Migration Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(MigrationAssessmentsRequest.Type, HandleMigrationAssessmentsRequest);
        }

        /// <summary>
        /// Handle request to start a migration session
        /// </summary>
        internal async Task HandleMigrationAssessmentsRequest(
            MigrationAssessmentsParams parameters, 
            RequestContext<MigrationAssessmentsResult> requestContext)
        {           
            try
            {                    
                var result = new MigrationAssessmentsResult();
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(new Exception(SR.CreateSessionFailed(e.Message)));
            }
        }

        /// <summary>
        /// Disposes the Migration Service
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
