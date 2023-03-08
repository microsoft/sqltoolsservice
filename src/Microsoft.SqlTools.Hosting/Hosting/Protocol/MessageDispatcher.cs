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
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public class MessageDispatcher
    {
        #region Fields

        private ChannelBase protocolChannel;

        private AsyncContextThread messageLoopThread;

        internal Dictionary<string, Func<Message, MessageWriter, Task>> requestHandlers =
            new Dictionary<string, Func<Message, MessageWriter, Task>>();

        internal Dictionary<string, bool> requestHandlerParallelismMap =
            new Dictionary<string, bool>();

        internal Dictionary<string, Func<Message, MessageWriter, Task>> eventHandlers =
            new Dictionary<string, Func<Message, MessageWriter, Task>>();

        internal Dictionary<string, bool> eventHandlerParallelismMap =
            new Dictionary<string, bool>();

        private Action<Message> responseHandler;

        private CancellationTokenSource messageLoopCancellationToken =
            new CancellationTokenSource();

        private SemaphoreSlim semaphore = new SemaphoreSlim(10); // Limit to 10 threads to begin with, ideally there shouldn't be any limitation

        #endregion

        #region Properties

        public SynchronizationContext SynchronizationContext { get; private set; }

        public bool InMessageLoopThread
        {
            get
            {
                // We're in the same thread as the message loop if the
                // current synchronization context equals the one we
                // know.
                return SynchronizationContext.Current == this.SynchronizationContext;
            }
        }

        protected MessageReader MessageReader { get; private set; }

        protected MessageWriter MessageWriter { get; private set; }


        /// <summary>
        /// Whether the message should be handled without blocking the main thread.
        /// </summary>
        public bool ParallelMessageProcessing { get; set; }


        #endregion

        #region Constructors

        public MessageDispatcher(ChannelBase protocolChannel)
        {
            this.protocolChannel = protocolChannel;
            this.MessageReader = protocolChannel.MessageReader;
            this.MessageWriter = protocolChannel.MessageWriter;
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            // Start the main message loop thread.  The Task is
            // not explicitly awaited because it is running on
            // an independent background thread.
            this.messageLoopThread = new AsyncContextThread("Message Dispatcher");
            this.messageLoopThread
                .Run(() => this.ListenForMessages(this.messageLoopCancellationToken.Token))
                .ContinueWith(this.OnListenTaskCompleted);
        }

        public void Stop()
        {
            // Stop the message loop thread
            if (this.messageLoopThread != null)
            {
                this.messageLoopCancellationToken.Cancel();
                this.messageLoopThread.Stop();
            }
        }

        public void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler)
        {
            this.SetRequestHandler(
                requestType,
                requestHandler,
                false);
        }

        public void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler,
            bool overrideExisting,
            bool isParallelProcessingSupported = false)
        {
            if (overrideExisting)
            {
                // Remove the existing handler so a new one can be set
                this.requestHandlers.Remove(requestType.MethodName);
            }

            this.requestHandlerParallelismMap.Add(requestType.MethodName, isParallelProcessingSupported);
            this.requestHandlers.Add(
                requestType.MethodName,
                async (requestMessage, messageWriter) =>
                {
                    Logger.Write(TraceEventType.Verbose, $"Processing message with id[{requestMessage.Id}], of type[{requestMessage.MessageType}] and method[{requestMessage.Method}]");
                    var requestContext =
                        new RequestContext<TResult>(
                            requestMessage,
                            messageWriter);
                    try
                    {
                        TParams typedParams = default(TParams);
                        if (requestMessage.Contents != null)
                        {
                            try
                            {
                                typedParams = requestMessage.Contents.ToObject<TParams>();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Error parsing message contents {requestMessage.Contents}", ex);
                            }
                        }

                        await requestHandler(typedParams, requestContext);
                        Logger.Write(TraceEventType.Verbose, $"Finished processing message with id[{requestMessage.Id}], of type[{requestMessage.MessageType}] and method[{requestMessage.Method}]");
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = GetErrorMessage(ex);
                        Logger.Error($"{requestType.MethodName} : {errorMessage}");
                        await requestContext.SendError(errorMessage);
                    }
                });
        }

        private string GetErrorMessage(Exception e)
        {
            string res = string.Empty;

            while (e != null)
            {
                res += e.Message + Environment.NewLine;
                e = e.InnerException;
            }

            return res.TrimEnd( Environment.NewLine.ToCharArray());;
        }

         public void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler)
        {
            this.SetEventHandler(
                eventType,
                eventHandler,
                false);
        }

        public void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool overrideExisting,
            bool isParallelProcessingSupported = false)
        {
            if (overrideExisting)
            {
                // Remove the existing handler so a new one can be set
                this.eventHandlers.Remove(eventType.MethodName);
            }

            this.eventHandlerParallelismMap.Add(eventType.MethodName, isParallelProcessingSupported);
            this.eventHandlers.Add(
                eventType.MethodName,
                async (eventMessage, messageWriter) =>
                {
                    Logger.Write(TraceEventType.Verbose, $"Processing message with id[{eventMessage.Id}], of type[{eventMessage.MessageType}] and method[{eventMessage.Method}]");
                    var eventContext = new EventContext(messageWriter);
                    TParams typedParams = default(TParams);
                    try
                    {
                        if (eventMessage.Contents != null)
                        {
                            try
                            {
                                typedParams = eventMessage.Contents.ToObject<TParams>();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Error parsing message contents {eventMessage.Contents}", ex);
                            }
                        }
                        await eventHandler(typedParams, eventContext);
                        Logger.Write(TraceEventType.Verbose, $"Finished processing message with id[{eventMessage.Id}], of type[{eventMessage.MessageType}] and method[{eventMessage.Method}]");
                    }
                    catch (Exception ex)
                    {
                        // There's nothing on the client side to send an error back to so just log the error and move on
                        Logger.Error($"{eventType.MethodName} : {ex}");
                    }
                });
        }

        public void SetResponseHandler(Action<Message> responseHandler)
        {
            this.responseHandler = responseHandler;
        }

        #endregion

        #region Events

        public event EventHandler<Exception> UnhandledException;

        protected void OnUnhandledException(Exception unhandledException)
        {
            if (this.UnhandledException != null)
            {
                this.UnhandledException(this, unhandledException);
            }
        }

        #endregion

        #region Private Methods

        private async Task ListenForMessages(CancellationToken cancellationToken)
        {
            this.SynchronizationContext = SynchronizationContext.Current;

            // Run the message loop
            while (!cancellationToken.IsCancellationRequested)
            {
                Message newMessage;

                try
                {
                    // Read a message from the channel
                    newMessage = await this.MessageReader.ReadMessage();
                }
                catch (MessageParseException e)
                {
                    string message = string.Format("Exception occurred while parsing message: {0}", e.Message);
                    Logger.Write(TraceEventType.Error, message);
                    await MessageWriter.WriteEvent(HostingErrorEvent.Type, new HostingErrorParams { Message = message });

                    // Continue the loop
                    continue;
                }
                catch (EndOfStreamException)
                {
                    // The stream has ended, end the message loop
                    break;
                }
                catch (Exception e)
                {
                    // Log the error and send an error event to the client
                    string message = string.Format("Exception occurred while receiving message: {0}", e.Message);
                    Logger.Write(TraceEventType.Error, message);
                    await MessageWriter.WriteEvent(HostingErrorEvent.Type, new HostingErrorParams { Message = message });

                    // Continue the loop
                    continue;
                }

                // The message could be null if there was an error parsing the
                // previous message.  In this case, do not try to dispatch it.
                if (newMessage != null)
                {
                    // Verbose logging
                    string logMessage =
                        $"Received message with id[{newMessage.Id}], of type[{newMessage.MessageType}] and method[{newMessage.Method}]";
                    Logger.Write(TraceEventType.Verbose, logMessage);

                    // Process the message
                    await this.DispatchMessage(newMessage, this.MessageWriter);
                }
            }
        }

        protected async Task DispatchMessage(
            Message messageToDispatch,
            MessageWriter messageWriter)
        {
            Func<Message, MessageWriter, Task> handlerToAwait = null;
            bool isParallelProcessingSupported = false;

            if (messageToDispatch.MessageType == MessageType.Request)
            {
                this.requestHandlers.TryGetValue(messageToDispatch.Method, out handlerToAwait);
                this.requestHandlerParallelismMap.TryGetValue(messageToDispatch.Method, out isParallelProcessingSupported);
            }
            else if (messageToDispatch.MessageType == MessageType.Response)
            {
                if (this.responseHandler != null)
                {
                    this.responseHandler(messageToDispatch);
                }
            }
            else if (messageToDispatch.MessageType == MessageType.Event)
            {
                this.eventHandlers.TryGetValue(messageToDispatch.Method, out handlerToAwait);
                this.eventHandlerParallelismMap.TryGetValue(messageToDispatch.Method, out isParallelProcessingSupported);
            }
            // else
            // {
            //     // TODO: Return message not supported
            // }

            if (handlerToAwait != null)
            {
                try
                {
                    if (this.ParallelMessageProcessing && isParallelProcessingSupported)
                    {
                        // Run the task in a separate thread so that the main
                        // thread is not blocked. Use semaphore to limit the degree of parallelism.
                        await semaphore.WaitAsync();
                        _ = Task.Run(async () =>
                        {
                            await handlerToAwait(messageToDispatch, messageWriter);
                            semaphore.Release();
                        });
                    }
                    else
                    {
                        await handlerToAwait(messageToDispatch, messageWriter);
                    }
                }
                catch (TaskCanceledException e)
                {
                    // Some tasks may be cancelled due to legitimate
                    // timeouts so don't let those exceptions go higher.
                    Logger.Write(TraceEventType.Verbose, string.Format("A TaskCanceledException occurred in the request handler: {0}", e.ToString()));
                }
                catch (Exception e)
                {
                    if (!(e is AggregateException exception && exception.InnerExceptions[0] is TaskCanceledException))
                    {
                        // Log the error but don't rethrow it to prevent any errors in the handler from crashing the service
                        Logger.Write(TraceEventType.Error, string.Format("An unexpected error occurred in the request handler: {0}", e.ToString()));
                    }
                }
            }
        }

        internal void OnListenTaskCompleted(Task listenTask)
        {
            if (listenTask.IsFaulted)
            {
                this.OnUnhandledException(listenTask.Exception);
            }
            else if (listenTask.IsCompleted || listenTask.IsCanceled)
            {
                // TODO: Dispose of anything?
            }
        }

        #endregion
    }
}

