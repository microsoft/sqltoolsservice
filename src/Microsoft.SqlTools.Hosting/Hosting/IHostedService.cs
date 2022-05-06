﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Hosting
{
    /// <summary>
    /// Defines a hosted service that communicates with external processes via
    /// messages passed over the <see cref="ServiceHost"/>. The service defines
    /// a standard initialization method where it can hook up to the host.
    /// </summary>
    public interface IHostedService 
    {
        /// <summary>
        /// Callback to initialize this service
        /// </summary>
        /// <param name="serviceHost"><see cref="IProtocolEndpoint"/> which supports registering
        /// event handlers and other callbacks for messages passed to external callers</param>
        void InitializeService(IProtocolEndpoint serviceHost);

        /// <summary>
        /// What is the service type that you wish to register?
        /// </summary>
        Type ServiceType { get; }
    }

    /// <summary>
    /// Base class for <see cref="IHostedService"/> implementations that handles defining the <see cref="ServiceType"/>
    /// being registered. This simplifies service registration. This also implements <see cref="IComposableService"/> which
    /// allows injection of the service provider for lookup of other services.
    /// 
    /// Extending classes should implement per below code example
    /// <code>
    /// [Export(typeof(IHostedService)]
    /// MyService : HostedService&lt;MyService&gt;
    /// {
    ///     public override void InitializeService(IProtocolEndpoint serviceHost)
    ///     {
    ///         serviceHost.SetRequestHandler(MyRequest.Type, HandleMyRequest);
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">Type to be registered for lookup in the service provider</typeparam>
    public abstract class HostedService<T> : IHostedService, IComposableService
    {

        protected IMultiServiceProvider ServiceProvider { get; private set; }

        public virtual void SetServiceProvider(IMultiServiceProvider provider)
        {
            ServiceProvider = provider;
        }

        public Type ServiceType
        {
            get
            {
                return typeof(T);
            }
        }

        protected async Task<THandler> HandleRequestAsync<THandler>(Func<Task<THandler>> handler, RequestContext<THandler> requestContext, string requestType)
        {
            Logger.Verbose($"Handling request type {requestType}");

            try
            {
                THandler result = await handler();
                await requestContext.SendResult(result);
                return result;
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
            return default(THandler);
        }

        public abstract void InitializeService(IProtocolEndpoint serviceHost);

    }
}
