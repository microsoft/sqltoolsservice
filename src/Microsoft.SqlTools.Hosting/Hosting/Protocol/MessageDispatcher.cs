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

        internal Dictionary<string, Func<Message, MessageWriter, Task>> eventHandlers =
            new Dictionary<string, Func<Message, MessageWriter, Task>>();

        private Action<Message> responseHandler;

        private CancellationTokenSource messageLoopCancellationToken =
            new CancellationTokenSource();

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
            bool overrideExisting)
        {
            if (overrideExisting)
            {
                // Remove the existing handler so a new one can be set
                this.requestHandlers.Remove(requestType.MethodName);
            }

            this.requestHandlers.Add(
                requestType.MethodName,
                (requestMessage, messageWriter) =>
                {
                    var requestContext =
                        new RequestContext<TResult>(
                            requestMessage,
                            messageWriter);

                    TParams typedParams = default(TParams);
                    if (requestMessage.Contents != null)
                    {
                        // TODO: Catch parse errors!
                        typedParams = requestMessage.Contents.ToObject<TParams>();
                    }

                    return requestHandler(typedParams, requestContext);
                });
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
            bool overrideExisting)
        {
            if (overrideExisting)
            {
                // Remove the existing handler so a new one can be set
                this.eventHandlers.Remove(eventType.MethodName);
            }

            this.eventHandlers.Add(
                eventType.MethodName,
                (eventMessage, messageWriter) =>
                {
                    var eventContext = new EventContext(messageWriter);

                    TParams typedParams = default(TParams);
                    if (eventMessage.Contents != null)
                    {
                        try
                        {
                            typedParams = eventMessage.Contents.ToObject<TParams>();
                        }
                        catch (Exception ex)
                        {
                            Logger.Write(TraceEventType.Verbose, ex.ToString());
                        }
                    }

                    return eventHandler(typedParams, eventContext);
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
                    string logMessage = string.Format("Received message of type[{0}] and method[{1}]",
                        newMessage.MessageType, newMessage.Method);
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
            Task handlerToAwait = null;

            if (messageToDispatch.MessageType == MessageType.Request)
            {
                Func<Message, MessageWriter, Task> requestHandler = null;
                if (this.requestHandlers.TryGetValue(messageToDispatch.Method, out requestHandler))
                {
                    handlerToAwait = requestHandler(messageToDispatch, messageWriter);
                }
                // else
                // {
                //     // TODO: Message not supported error
                // }
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
                Func<Message, MessageWriter, Task> eventHandler = null;
                if (this.eventHandlers.TryGetValue(messageToDispatch.Method, out eventHandler))
                {
                    handlerToAwait = eventHandler(messageToDispatch, messageWriter);
                }
                else
                {
                    // TODO: Message not supported error
                }
            }
            // else
            // {
            //     // TODO: Return message not supported
            // }

            if (handlerToAwait != null)
            {
                try
                {
                    await handlerToAwait;
                }
                catch (TaskCanceledException)
                {
                    // Some tasks may be cancelled due to legitimate
                    // timeouts so don't let those exceptions go higher.
                }
                catch (Exception e)
                {
                    if (!(e is AggregateException && ((AggregateException)e).InnerExceptions[0] is TaskCanceledException))
                    {
                        // Log the error but don't rethrow it to prevent any errors in the handler from crashing the service
                        Logger.Write(TraceEventType.Error, string.Format("An unexpected error occured in the request handler: {0}", e.ToString()));
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

