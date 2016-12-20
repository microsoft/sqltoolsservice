//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Channel;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.JsonRpc.Driver
{
    /// <summary>
    /// Wraps the ProtocolEndpoint class with queues to handle events/requests
    /// </summary>
    public class ClientDriverBase
    {
        protected ProtocolEndpoint protocolClient;

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
            return 
                this.protocolClient.SendRequest(
                    requestType, 
                    requestParams);
        }

        public Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            return 
                this.protocolClient.SendEvent(
                    eventType,
                    eventParams);
        }

        public void QueueEventsForType<TParams>(EventType<TParams> eventType)
        {
            var eventQueue =
                this.eventQueuePerType.AddOrUpdate(
                    eventType.MethodName,
                    new AsyncQueue<object>(),
                    (key, queue) => queue);

            this.protocolClient.SetEventHandler(
                eventType,
                (p, ctx) =>
                {
                    return eventQueue.EnqueueAsync(p);   
                });
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

                this.protocolClient.SetEventHandler(
                    eventType,
                    (p, ctx) =>
                    {
                        if (!eventTaskSource.Task.IsCompleted)
                        {
                            eventTaskSource.SetResult(p);
                        }

                        return Task.FromResult(true);
                    },
                    true);  // Override any existing handler

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

        public async Task<Tuple<TParams, RequestContext<TResponse>>> WaitForRequest<TParams, TResponse>(
            RequestType<TParams, TResponse> requestType,
            int timeoutMilliseconds = 5000)
        {
            Task<Tuple<TParams, RequestContext<TResponse>>> requestTask = null;

            // Use the request queue if one has been registered
            AsyncQueue<object> requestQueue = null;
            if (this.requestQueuePerType.TryGetValue(requestType.MethodName, out requestQueue))
            {
                requestTask =
                    requestQueue
                        .DequeueAsync()
                        .ContinueWith(
                            task => (Tuple<TParams, RequestContext<TResponse>>)task.Result);
            }
            else
            {
                var requestTaskSource =
                    new TaskCompletionSource<Tuple<TParams, RequestContext<TResponse>>>();

                this.protocolClient.SetRequestHandler(
                    requestType,
                    (p, ctx) =>
                    {
                        if (!requestTaskSource.Task.IsCompleted)
                        {
                            requestTaskSource.SetResult(
                                new Tuple<TParams, RequestContext<TResponse>>(p, ctx));
                        }

                        return Task.FromResult(true);
                    });

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
    }
}

