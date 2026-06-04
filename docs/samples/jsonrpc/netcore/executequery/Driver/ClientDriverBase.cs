//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StreamJsonRpc;

namespace Microsoft.SqlTools.JsonRpc.Driver
{
    public sealed class PendingRequest<TParams, TResponse>
    {
        internal PendingRequest(TParams parameters)
        {
            this.Parameters = parameters;
            this.ResponseSource = new TaskCompletionSource<TResponse>();
        }

        public TParams Parameters { get; }

        public TaskCompletionSource<TResponse> ResponseSource { get; }

        public Task SetResult(TResponse response)
        {
            this.ResponseSource.TrySetResult(response);
            return Task.CompletedTask;
        }

        public Task SetException(Exception exception)
        {
            this.ResponseSource.TrySetException(exception);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Wraps a StreamJsonRpc client with queues to handle events/requests.
    /// </summary>
    public class ClientDriverBase
    {
        protected StreamJsonRpc.JsonRpc jsonRpc;

        protected StdioClientChannel clientChannel;

        private ConcurrentDictionary<string, AsyncQueue<object>> eventQueuePerType =
            new ConcurrentDictionary<string, AsyncQueue<object>>();

        private ConcurrentDictionary<string, AsyncQueue<object>> requestQueuePerType =
            new ConcurrentDictionary<string, AsyncQueue<object>>();

        public Process ServiceProcess
        {
            get
            {
                try
                {
                    return Process.GetProcessById(clientChannel.ProcessId);
                }
                catch
                {
                    return null;
                }
            }
        }

        public Task<TResult> SendRequest<TParams, TResult>(
            RequestType<TParams, TResult> requestType, 
            TParams requestParams)
        {
            return this.jsonRpc.InvokeWithParameterObjectAsync<TResult>(
                requestType.MethodName,
                requestParams);
        }

        public Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            return this.jsonRpc.NotifyWithParameterObjectAsync(
                eventType.MethodName,
                eventParams);
        }

        protected void InitializeRpcClient()
        {
            this.clientChannel.Start();
            this.jsonRpc = new StreamJsonRpc.JsonRpc(CreateMessageHandler(this.clientChannel.OutputStream, this.clientChannel.InputStream))
            {
                AllowModificationWhileListening = true,
                CancelLocallyInvokedMethodsWhenConnectionIsClosed = true
            };
        }

        protected async Task StartRpcClient()
        {
            await this.clientChannel.WaitForConnection();
            this.jsonRpc.StartListening();
        }

        protected Task StopRpcClient()
        {
            this.jsonRpc?.Dispose();
            this.clientChannel.Stop();
            return Task.CompletedTask;
        }

        public void QueueEventsForType<TParams>(EventType<TParams> eventType)
        {
            var eventQueue =
                this.eventQueuePerType.AddOrUpdate(
                    eventType.MethodName,
                    new AsyncQueue<object>(),
                    (key, queue) => queue);

            this.jsonRpc.AddLocalRpcMethod(
                eventType.MethodName,
                new Func<TParams, Task>(p =>
                {
                    return eventQueue.EnqueueAsync(p);   
                }));
        }

        public async Task<TParams> WaitForEvent<TParams>(
            EventType<TParams> eventType,
            int timeoutMilliseconds = 5000)
        {
            Task<TParams> eventTask = null;

            // Use the event queue if one has been registered
            AsyncQueue<object> eventQueue = null;
            if (this.eventQueuePerType.TryGetValue(eventType.MethodName, out eventQueue))
            {
                eventTask =
                    eventQueue
                        .DequeueAsync()
                        .ContinueWith<TParams>(
                            task => (TParams)task.Result);
            }
            else
            {
                TaskCompletionSource<TParams> eventTaskSource = new TaskCompletionSource<TParams>();

                this.jsonRpc.AddLocalRpcMethod(
                    eventType.MethodName,
                    new Func<TParams, Task>(p =>
                    {
                        if (!eventTaskSource.Task.IsCompleted)
                        {
                            eventTaskSource.SetResult(p);
                        }

                        return Task.FromResult(true);
                    }));

                eventTask = eventTaskSource.Task;
            }

            await 
                Task.WhenAny(
                    eventTask,
                    Task.Delay(timeoutMilliseconds));

            if (!eventTask.IsCompleted)
            {
                throw new TimeoutException(
                    string.Format(
                        "Timed out waiting for '{0}' event!",
                        eventType.MethodName));
            }

            return await eventTask;
        }

        public async Task<PendingRequest<TParams, TResponse>> WaitForRequest<TParams, TResponse>(
            RequestType<TParams, TResponse> requestType,
            int timeoutMilliseconds = 5000)
        {
            Task<PendingRequest<TParams, TResponse>> requestTask = null;

            // Use the request queue if one has been registered
            AsyncQueue<object> requestQueue = null;
            if (this.requestQueuePerType.TryGetValue(requestType.MethodName, out requestQueue))
            {
                requestTask =
                    requestQueue
                        .DequeueAsync()
                        .ContinueWith(
                            task => (PendingRequest<TParams, TResponse>)task.Result);
            }
            else
            {
                var requestTaskSource =
                    new TaskCompletionSource<PendingRequest<TParams, TResponse>>();

                this.jsonRpc.AddLocalRpcMethod(
                    requestType.MethodName,
                    new Func<TParams, Task<TResponse>>(p =>
                    {
                        PendingRequest<TParams, TResponse> pendingRequest =
                            new PendingRequest<TParams, TResponse>(p);

                        if (!requestTaskSource.Task.IsCompleted)
                        {
                            requestTaskSource.SetResult(pendingRequest);
                        }

                        return pendingRequest.ResponseSource.Task;
                    }));

                requestTask = requestTaskSource.Task;
            }

            await 
                Task.WhenAny(
                    requestTask,
                    Task.Delay(timeoutMilliseconds));

            if (!requestTask.IsCompleted)
            {
                throw new TimeoutException(
                    string.Format(
                        "Timed out waiting for '{0}' request!",
                        requestType.MethodName));
            }

            return await requestTask;
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
    }
}

