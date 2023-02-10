//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    /// <summary>
    /// Provides behavior for a client or server endpoint that
    /// communicates using the specified protocol.
    /// </summary>
    public class ProtocolEndpoint : IProtocolEndpoint
    {
        private bool isInitialized;
        private bool isStarted;
        private int currentMessageId;
        private ChannelBase protocolChannel;
        private MessageProtocolType messageProtocolType;
        private TaskCompletionSource<bool> endpointExitedTask;
        private SynchronizationContext originalSynchronizationContext;

        private Dictionary<string, TaskCompletionSource<Message>> pendingRequests =
            new Dictionary<string, TaskCompletionSource<Message>>();

        /// <summary>
        /// When true, SendEvent will ignore exceptions and write them
        /// to the log instead. Intended to be used for test scenarios
        /// where SendEvent throws exceptions unrelated to what is
        /// being tested.
        /// </summary>
        internal static bool SendEventIgnoreExceptions = false;

        /// <summary>
        /// Gets the MessageDispatcher which allows registration of
        /// handlers for requests, responses, and events that are
        /// transmitted through the channel.
        /// </summary>
        internal MessageDispatcher MessageDispatcher { get; set; }

        /// <summary>
        /// Initializes an instance of the protocol server using the
        /// specified channel for communication.
        /// </summary>
        /// <param name="protocolChannel">
        /// The channel to use for communication with the connected endpoint.
        /// </param>
        /// <param name="messageProtocolType">
        /// The type of message protocol used by the endpoint.
        /// </param>
        public ProtocolEndpoint(
            ChannelBase protocolChannel,
            MessageProtocolType messageProtocolType)
        {
            this.protocolChannel = protocolChannel;
            this.messageProtocolType = messageProtocolType;
            this.originalSynchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Initializes
        /// </summary>
        public void Initialize(Stream inputStream = null, Stream outputStream = null)
        {
            if (!this.isInitialized)
            {
                // Start the provided protocol channel
                this.protocolChannel.Start(this.messageProtocolType, inputStream, outputStream);

                // Start the message dispatcher
                this.MessageDispatcher = new MessageDispatcher(this.protocolChannel);

                // Set the handler for any message responses that come back
                this.MessageDispatcher.SetResponseHandler(this.HandleResponse);

                // Listen for unhandled exceptions from the dispatcher
                this.MessageDispatcher.UnhandledException += MessageDispatcher_UnhandledException;

                this.isInitialized = true;
            }
        }

        /// <summary>
        /// Starts the language server client and sends the Initialize method.
        /// </summary>
        /// <returns>A Task that can be awaited for initialization to complete.</returns>
        public async Task Start()
        {
            if (!this.isStarted)
            {

                // Notify implementation about endpoint start
                await this.OnStart();

                // Wait for connection and notify the implementor
                // NOTE: This task is not meant to be awaited.
                Task waitTask =
                    this.protocolChannel
                        .WaitForConnection()
                        .ContinueWith(
                            async (t) =>
                            {
                                // Start the MessageDispatcher
                                this.MessageDispatcher.Start();
                                await this.OnConnect();
                            });

                // Endpoint is now started
                this.isStarted = true;
            }
        }

        public async Task WaitForExitAsync()
        {
            this.endpointExitedTask = new TaskCompletionSource<bool>();
            await this.endpointExitedTask.Task.WaitAsync(CancellationToken.None);
        }

        public async Task Stop()
        {
            if (this.isStarted)
            {
                // Make sure no future calls try to stop the endpoint during shutdown
                this.isStarted = false;

                // Stop the implementation first
                await this.OnStop();

                // Stop the dispatcher and channel
                this.MessageDispatcher.Stop();
                this.protocolChannel.Stop();

                // Notify anyone waiting for exit
                if (this.endpointExitedTask != null)
                {
                    this.endpointExitedTask.SetResult(true);
                }
            }
        }

        #region Message Sending

        /// <summary>
        /// Sends a request to the server
        /// </summary>
        /// <typeparam name="TParams"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="requestType"></param>
        /// <param name="requestParams"></param>
        /// <returns></returns>
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
            if (!this.protocolChannel.IsConnected)
            {
                throw new InvalidOperationException("SendRequest called when ProtocolChannel was not yet connected");
            }

            this.currentMessageId++;

            TaskCompletionSource<Message> responseTask = null;

            if (waitForResponse)
            {
                responseTask = new TaskCompletionSource<Message>();
                this.pendingRequests.Add(
                    this.currentMessageId.ToString(), 
                    responseTask);
            }

            await this.protocolChannel.MessageWriter.WriteRequest<TParams, TResult>(
                requestType, 
                requestParams, 
                this.currentMessageId);

            if (responseTask != null)
            {
                var responseMessage = await responseTask.Task;

                return
                    responseMessage.Contents != null ?
                        responseMessage.Contents.ToObject<TResult>() :
                        default(TResult);
            }
            else
            {
                // TODO: Better default value here?
                return default(TResult);
            }
        }

        /// <summary>
        /// Sends an event to the channel's endpoint.
        /// </summary>
        /// <typeparam name="TParams">The event parameter type.</typeparam>
        /// <param name="eventType">The type of event being sent.</param>
        /// <param name="eventParams">The event parameters being sent.</param>
        /// <returns>A Task that tracks completion of the send operation.</returns>
        public Task SendEvent<TParams>(
            EventType<TParams> eventType,
            TParams eventParams)
        {
            try
            {
                if (!this.protocolChannel.IsConnected)
                {
                    throw new InvalidOperationException("SendEvent called when ProtocolChannel was not yet connected");
                }

                // Some events could be raised from a different thread.
                // To ensure that messages are written serially, dispatch
                // dispatch the SendEvent call to the message loop thread.

                if (!this.MessageDispatcher.InMessageLoopThread)
                {
                    TaskCompletionSource<bool> writeTask = new TaskCompletionSource<bool>();

                    this.MessageDispatcher.SynchronizationContext.Post(
                        async (obj) =>
                        {
                            await this.protocolChannel.MessageWriter.WriteEvent(
                                eventType,
                                eventParams);

                            writeTask.SetResult(true);
                        }, null);

                    return writeTask.Task;
                }
                else
                {
                    return this.protocolChannel.MessageWriter.WriteEvent(
                        eventType,
                        eventParams);
                }
            }
            catch (Exception ex)
            {
                if (SendEventIgnoreExceptions)
                {
                    Logger.Write(TraceEventType.Verbose, "Exception in SendEvent " + ex.ToString());
                }
                else
                {
                    throw;
                }
            }
            return Task.FromResult(false);
        }

        #endregion

        #region Message Handling

        public void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler,
            bool isParallelProcessingSupported = false)
        {
            this.MessageDispatcher.SetRequestHandler(
                requestType,
                requestHandler,
                false,
                isParallelProcessingSupported);
        }

        public void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool isParallelProcessingSupported = false)
        {
            this.MessageDispatcher.SetEventHandler(
                eventType,
                eventHandler,
                false,
                isParallelProcessingSupported);
        }

        public void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool overrideExisting,
            bool isParallelProcessingSupported = false)
        {
            this.MessageDispatcher.SetEventHandler(
                eventType,
                eventHandler,
                overrideExisting,
                isParallelProcessingSupported);
        }

        private void HandleResponse(Message responseMessage)
        {
            TaskCompletionSource<Message> pendingRequestTask = null;

            if (this.pendingRequests.TryGetValue(responseMessage.Id, out pendingRequestTask))
            {
                pendingRequestTask.SetResult(responseMessage);
                this.pendingRequests.Remove(responseMessage.Id);
            }
        }

        #endregion

        #region Subclass Lifetime Methods

        protected virtual Task OnStart()
        {
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

        #endregion

        #region Event Handlers

        private void MessageDispatcher_UnhandledException(object sender, Exception e)
        {
            if (this.endpointExitedTask != null)
            {
                this.endpointExitedTask.SetException(e);
            }

            else if (this.originalSynchronizationContext != null)
            {
                this.originalSynchronizationContext.Post(o => { throw e; }, null);
            }
        }

        #endregion
    }
}

