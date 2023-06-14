//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;

using Microsoft.SqlTools.ServiceLayer.Hosting;

namespace Microsoft.SqlTools.ServiceLayer.Diagnostics
{
    /// <summary>
    /// Main class for Diagnostics Service functionality
    /// </summary>
    public sealed class DiagnosticsService : IDisposable
    {
        private static readonly Lazy<DiagnosticsService> instance = new Lazy<DiagnosticsService>(() => new DiagnosticsService());

        private bool disposed;

        /// <summary>
        /// Construct a new Execution Plan Service instance with default parameters
        /// </summary>
        private DiagnosticsService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static DiagnosticsService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost { get; set; }

        /// <summary>
        /// Initializes the Execution Plan Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            ServiceHost = serviceHost;
            ServiceHost.SetRequestHandler(GetDiagnosticsRequest.Type, HandleGetDiagnostics, true);
        }

        private async Task HandleGetDiagnostics(GetDiagnosticsParams requestParams, RequestContext<GetDiagnosticsResult> requestContext)
        {
            await requestContext.SendResult(new GetDiagnosticsResult
            {
                recommendation = "test"
            });
        }

        /// <summary>
        /// Disposes the Execution Plan Service
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
