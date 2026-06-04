//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StreamJsonRpc;

namespace Microsoft.SqlTools.Hosting
{
    public abstract class ServiceHostBase : IRpcServiceHost, IDisposable
    {
        private readonly ChannelBase serverChannel;
        private readonly SynchronizationContext originalSynchronizationContext;

        private bool isInitialized;
        private bool isStarted;
        private JsonRpc jsonRpc;
        private TaskCompletionSource<bool> serverExitedTask;
        private TaskCompletionSource<bool> endpointExitedTask;

        internal static bool SendEventIgnoreExceptions = false;

        protected ServiceHostBase(ChannelBase serverChannel)
        {
            this.serverChannel = serverChannel;
            this.originalSynchronizationContext = SynchronizationContext.Current;
        }

        public void Initialize(Stream inputStream = null, Stream outputStream = null)
        {
            if (this.isInitialized)
            {
                return;
            }

            this.serverChannel.Start(inputStream, outputStream);
            this.jsonRpc = new JsonRpc(CreateMessageHandler(this.serverChannel.OutputStream, this.serverChannel.InputStream))
            {
                AllowModificationWhileListening = true,
                CancelLocallyInvokedMethodsWhenConnectionIsClosed = true
            };
            this.jsonRpc.Disconnected += this.JsonRpc_Disconnected;

            this.isInitialized = true;
        }

        public async Task Start()
        {
            if (this.isStarted)
            {
                return;
            }

            await this.OnStart();
            await this.serverChannel.WaitForConnection();

            this.jsonRpc.StartListening();
            await this.OnConnect();

            this.isStarted = true;
        }

        public async Task WaitForExitAsync()
        {
            this.endpointExitedTask = new TaskCompletionSource<bool>();

            if (this.jsonRpc != null)
            {
                await this.jsonRpc.Completion;
                return;
            }

#if !NET8_0_OR_GREATER
            await this.endpointExitedTask.Task;
#else
            await this.endpointExitedTask.Task.WaitAsync(CancellationToken.None);
#endif
        }

        public async Task Stop()
        {
            if (!this.isStarted)
            {
                return;
            }

            this.isStarted = false;

            await this.OnStop();

            this.jsonRpc?.Dispose();
            this.serverChannel.Stop();
            this.endpointExitedTask?.TrySetResult(true);
        }

        public void Dispose()
        {
            this.jsonRpc?.Dispose();
        }

        public Task<TResult> SendRequest<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            TParams requestParams)
        {
            return this.SendRequest(requestType, requestParams, true);
        }

        public async Task<TResult> SendRequest<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            TParams requestParams,
            bool waitForResponse)
        {
            this.ThrowIfNotConnected(nameof(SendRequest));

            if (waitForResponse)
            {
                return await this.jsonRpc.InvokeWithParameterObjectAsync<TResult>(
                    requestType.MethodName,
                    requestParams).ConfigureAwait(false);
            }

            await this.jsonRpc.NotifyWithParameterObjectAsync(
                requestType.MethodName,
                requestParams).ConfigureAwait(false);
            return default(TResult);
        }

        public async Task SendEvent<TParams>(
            EventType<TParams> eventType,
            TParams eventParams)
        {
            try
            {
                this.ThrowIfNotConnected(nameof(SendEvent));
                await this.jsonRpc.NotifyWithParameterObjectAsync(
                    eventType.MethodName,
                    eventParams).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (SendEventIgnoreExceptions)
                {
                    Logger.Verbose("Exception in SendEvent " + ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public void RegisterRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, Task<TResult>> requestHandler)
        {
            this.ThrowIfNotInitialized(nameof(RegisterRequestHandler));

            this.jsonRpc.AddLocalRpcMethod(
                requestType.MethodName,
                new Func<TParams, Task<TResult>>(async requestParams =>
                {
                    Logger.Verbose($"Processing request method[{requestType.MethodName}]");
                    TResult result = await requestHandler(requestParams).ConfigureAwait(false);
                    Logger.Verbose($"Finished processing request method[{requestType.MethodName}]");
                    return result;
                }));
        }

        public void RegisterNotificationHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, Task> eventHandler)
        {
            this.ThrowIfNotInitialized(nameof(RegisterNotificationHandler));

            this.jsonRpc.AddLocalRpcMethod(
                eventType.MethodName,
                new Func<TParams, Task>(async eventParams =>
                {
                    Logger.Verbose($"Processing notification method[{eventType.MethodName}]");
                    await eventHandler(eventParams).ConfigureAwait(false);
                    Logger.Verbose($"Finished processing notification method[{eventType.MethodName}]");
                }));
        }

        protected virtual Task OnStart()
        {
            this.RegisterNotificationHandler(ExitNotification.Type, this.HandleExitNotification);
            this.serverExitedTask = new TaskCompletionSource<bool>();

            return Task.FromResult(true);
        }

        protected virtual Task OnConnect()
        {
            return Task.FromResult(true);
        }

        protected virtual Task OnStop()
        {
            return Task.FromResult(true);
        }

        private async Task HandleExitNotification(object exitParams)
        {
            await this.Stop();
            this.serverExitedTask?.TrySetResult(true);
        }

        private static HeaderDelimitedMessageHandler CreateMessageHandler(Stream outputStream, Stream inputStream)
        {
            var formatter = new JsonMessageFormatter(Encoding.UTF8);
            formatter.JsonSerializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            formatter.JsonSerializer.DateParseHandling = DateParseHandling.None;
            formatter.JsonSerializer.NullValueHandling = NullValueHandling.Include;
            formatter.JsonSerializer.TypeNameHandling = TypeNameHandling.None;

            return new HeaderDelimitedMessageHandler(outputStream, inputStream, formatter);
        }

        private void ThrowIfNotInitialized(string operationName)
        {
            if (!this.isInitialized || this.jsonRpc == null)
            {
                throw new InvalidOperationException($"{operationName} called when service host was not initialized");
            }
        }

        private void ThrowIfNotConnected(string operationName)
        {
            this.ThrowIfNotInitialized(operationName);

            if (!this.serverChannel.IsConnected)
            {
                throw new InvalidOperationException($"{operationName} called when service host channel was not yet connected");
            }
        }

        private void JsonRpc_Disconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            if (e.Exception != null)
            {
                if (this.endpointExitedTask != null)
                {
                    this.endpointExitedTask.TrySetException(e.Exception);
                }
                else if (this.originalSynchronizationContext != null)
                {
                    this.originalSynchronizationContext.Post(o => { throw e.Exception; }, null);
                }

                return;
            }

            this.endpointExitedTask?.TrySetResult(true);
        }
    }
}
