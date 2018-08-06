//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.SqlTools.Hosting.Extensibility
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
        /// <param name="serviceHost"><see cref="IServiceHost"/> which supports registering
        /// event handlers and other callbacks for messages passed to external callers</param>
        void InitializeService(IServiceHost serviceHost);

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
    ///     public override void InitializeService(IServiceHost serviceHost)
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

        public Type ServiceType => typeof(T);

        protected async Task<THandler> HandleRequestAsync<THandler>(
            Func<Task<THandler>> handler, 
            RequestContext<THandler> requestContext, 
            string requestType)
        {
            Logger.Instance.Write(LogLevel.Verbose, requestType);

            try
            {
                THandler result = await handler();
                requestContext.SendResult(result);
                return result;
            }
            catch (Exception ex)
            {
                requestContext.SendError(ex.ToString());
            }
            return default(THandler);
        }
        protected async Task<THandler> HandleSyncRequestAsAsync<THandler>(
            Func<THandler> handler, 
            RequestContext<THandler> requestContext, 
            string requestType)
        {
            Logger.Instance.Write(LogLevel.Verbose, requestType);
            return await Task.Factory.StartNew(() => {
                try
                {
                    THandler result = handler();
                    requestContext.SendResult(result);
                    return result;
                }
                catch (Exception ex)
                {
                    requestContext.SendError(ex.ToString());
                }
                return default(THandler);
            });
        }


        public abstract void InitializeService(IServiceHost serviceHost);

    }
}
