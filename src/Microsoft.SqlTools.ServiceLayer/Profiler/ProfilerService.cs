//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Main class for Profiler Service functionality
    /// </summary>
    public sealed class ProfilerService : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Initializes the Profiler Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(StartProfilingRequest.Type, HanldeStartProfilingRequest);
        }
        
        /// <summary>
        /// 
        /// </summary>
        internal async Task HanldeStartProfilingRequest(StartProfilingParams parameters, RequestContext<StartProfilingResult> requestContext)
        {
            try
            {             
                await requestContext.SendResult(new StartProfilingResult { SessionId = "abc" });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
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
