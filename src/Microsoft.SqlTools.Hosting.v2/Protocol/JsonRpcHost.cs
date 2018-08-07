//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Channels;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Contracts.Internal;
using Microsoft.SqlTools.Hosting.Utility;
using Microsoft.SqlTools.Hosting.v2;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public class JsonRpcHost : IJsonRpcHost
    {       
        #region Private Fields

        internal readonly CancellationTokenSource cancellationTokenSource;
        
        private readonly CancellationToken consumeInputCancellationToken;
        private readonly CancellationToken consumeOutputCancellationToken;

        internal readonly BlockingCollection<Message> outputQueue;
        internal readonly Dictionary<string, Func<Message, Task>> eventHandlers;
        internal readonly Dictionary<string, Func<Message, Task>> requestHandlers;
        internal readonly ConcurrentDictionary<string, TaskCompletionSource<Message>> pendingRequests;
        internal readonly ChannelBase protocolChannel;

        internal Task consumeInputTask;
        internal Task consumeOutputTask;
        private bool isStarted;
        
        #endregion
        
        public JsonRpcHost(ChannelBase channel)
        {
            Validate.IsNotNull(nameof(channel), channel);
            
            cancellationTokenSource = new CancellationTokenSource();
            consumeInputCancellationToken = cancellationTokenSource.Token;
            consumeOutputCancellationToken = cancellationTokenSource.Token;
            outputQueue = new BlockingCollection<Message>(new ConcurrentQueue<Message>());
            protocolChannel = channel;
            
            eventHandlers = new Dictionary<string, Func<Message, Task>>();
            requestHandlers = new Dictionary<string, Func<Message, Task>>();
            pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<Message>>();
        }
        
        #region Start/Stop Methods
        
        /// <summary>
        /// Starts the JSON RPC host using the protocol channel that was provided
        /// </summary>
        public void Start()
        {
            // If we've already started, we can't start up again
            if (isStarted)
            {
                throw new InvalidOperationException(SR.HostingJsonRpcHostAlreadyStarted);
            }
            
            // Make sure no other calls try to start the endpoint during startup
            isStarted = true;
            
            // Initialize the protocol channel
            protocolChannel.Start();
            protocolChannel.WaitForConnection().Wait();
            
            // Start the input and output consumption threads
            consumeInputTask = ConsumeInput();
            consumeOutputTask = ConsumeOutput();
        }

        /// <summary>
        /// Stops the JSON RPC host and the underlying protocol channel
        /// </summary>
        public void Stop()
        {
            // If we haven't started, we can't stop
            if (!isStarted)
            {
                throw new InvalidOperationException(SR.HostingJsonRpcHostNotStarted);
            }
            
            // Make sure no future calls try to stop the endpoint during shutdown
            isStarted = false;
            
            // Shutdown the host
            cancellationTokenSource.Cancel();
            protocolChannel.Stop();
        }

        /// <summary>
        /// Waits for input and output threads to naturally exit
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the host has not started</exception>
        public void WaitForExit()
        {
            // If we haven't started everything, we can't wait for exit
            if (!isStarted)
            {
                throw new InvalidOperationException(SR.HostingJsonRpcHostNotStarted);
            }
            
            // Join the input and output threads to this thread
            Task.WaitAll(consumeInputTask, consumeOutputTask);
        }
        
        #endregion
        
        #region Public Methods

        /// <summary>
        /// Sends an event, independent of any request
        /// </summary>
        /// <typeparam name="TParams">Event parameter type</typeparam>
        /// <param name="eventType">Type of event being sent</param>
        /// <param name="eventParams">Event parameters being sent</param>
        /// <returns>Task that tracks completion of the send operation.</returns>
        public void SendEvent<TParams>(
            EventType<TParams> eventType, 
            TParams eventParams)
        {
            if (!protocolChannel.IsConnected)
            {
                throw new InvalidOperationException("SendEvent called when ProtocolChannel was not yet connected");
            }
            
            // Create a message from the event provided
            Message message = Message.CreateEvent(eventType, eventParams);
            outputQueue.Add(message);
        }

        /// <summary>
        /// Sends a request, independent of any request
        /// </summary>
        /// <param name="requestType">Configuration of the request that is being sent</param>
        /// <param name="requestParams">Contents of the request</param>
        /// <typeparam name="TParams">Type of the message contents</typeparam>
        /// <typeparam name="TResult">Type of the contents of the expected result of the request</typeparam>
        /// <returns>Task that is completed when the </returns>
        /// TODO: This doesn't properly handle error responses scenarios.
        public async Task<TResult> SendRequest<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            TParams requestParams)
        {
            if (!protocolChannel.IsConnected)
            {
                throw new InvalidOperationException("SendRequest called when ProtocolChannel was not yet connected");
            }

            // Add a task completion source for the request's response
            string messageId = Guid.NewGuid().ToString();
            TaskCompletionSource<Message> responseTask = new TaskCompletionSource<Message>();
            pendingRequests.TryAdd(messageId, responseTask);
            
            // Send the request
            outputQueue.Add(Message.CreateRequest(requestType, messageId, requestParams));
            
            // Wait for the response
            Message responseMessage = await responseTask.Task;

            return responseMessage.GetTypedContents<TResult>();
        }
        
        /// <summary>
        /// Sets the handler for an event with a given configuration
        /// </summary>
        /// <param name="eventType">Configuration of the event</param>
        /// <param name="eventHandler">Function for handling the event</param>
        /// <param name="overrideExisting">Whether or not to override any existing event handler for this method</param>
        /// <typeparam name="TParams">Type of the parameters for the event</typeparam>
        public void SetAsyncEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool overrideExisting = false)
        {
            Validate.IsNotNull(nameof(eventType), eventType);
            Validate.IsNotNull(nameof(eventHandler), eventHandler);
            
            if (overrideExisting)
            {
                // Remove the existing handler so a new one can be set
                eventHandlers.Remove(eventType.MethodName);
            }

            Func<Message, Task> handler = eventMessage =>
                eventHandler(eventMessage.GetTypedContents<TParams>(), new EventContext(outputQueue));
            
            eventHandlers.Add(eventType.MethodName, handler);
        }

        /// <summary>
        /// Creates a Func based that wraps the action in a task and calls the Func-based overload
        /// </summary>
        /// <param name="eventType">Configuration of the event</param>
        /// <param name="eventHandler">Function for handling the event</param>
        /// <param name="overrideExisting">Whether or not to override any existing event handler for this method</param>
        /// <typeparam name="TParams">Type of the parameters for the event</typeparam>
        public void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Action<TParams, EventContext> eventHandler,
            bool overrideExisting = false)
        {
            Validate.IsNotNull(nameof(eventHandler), eventHandler);
            Func<TParams, EventContext, Task> eventFunc = (p, e) => Task.Run(() => eventHandler(p, e));
            SetAsyncEventHandler(eventType, eventFunc, overrideExisting);
        }
        
        /// <summary>
        /// Sets the handler for a request with a given configuration
        /// </summary>
        /// <param name="requestType">Configuration of the request</param>
        /// <param name="requestHandler">Function for handling the request</param>
        /// <param name="overrideExisting">Whether or not to override any existing request handler for this method</param>
        /// <typeparam name="TParams">Type of the parameters for the request</typeparam>
        /// <typeparam name="TResult">Type of the parameters for the response</typeparam>
        public void SetAsyncRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler,
            bool overrideExisting = false)
        {
            Validate.IsNotNull(nameof(requestType), requestType);
            Validate.IsNotNull(nameof(requestHandler), requestHandler);
            
            if (overrideExisting)
            {
                // Remove the existing handler so a new one can be set
                requestHandlers.Remove(requestType.MethodName);
            }
            
            // Setup the wrapper around the handler
            Func<Message, Task> handler = requestMessage =>
                requestHandler(requestMessage.GetTypedContents<TParams>(), new RequestContext<TResult>(requestMessage, outputQueue));
            
            requestHandlers.Add(requestType.MethodName, handler);
        }

        /// <summary>
        /// Creates a Func based that wraps the action in a task and calls the Func-based overload
        /// </summary>
        /// /// <param name="requestType">Configuration of the request</param>
        /// <param name="requestHandler">Function for handling the request</param>
        /// <param name="overrideExisting">Whether or not to override any existing request handler for this method</param>
        /// <typeparam name="TParams">Type of the parameters for the request</typeparam>
        /// <typeparam name="TResult">Type of the parameters for the response</typeparam>
        public void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Action<TParams, RequestContext<TResult>> requestHandler,
            bool overrideExisting = false)
        {
            Validate.IsNotNull(nameof(requestHandler), requestHandler);
            Func<TParams, RequestContext<TResult>, Task> requestFunc = (p, e) => Task.Run(() => requestHandler(p, e));
            SetAsyncRequestHandler(requestType, requestFunc, overrideExisting);
        }
        
        #endregion
        
        #region Message Processing Tasks

        internal Task ConsumeInput()
        {
            return Task.Factory.StartNew(async () =>
            {
                while (!consumeInputCancellationToken.IsCancellationRequested)
                {
                    Message incomingMessage;
                    try
                    {
                        // Read message from the input channel
                        incomingMessage = await protocolChannel.MessageReader.ReadMessage();
                    }
                    catch (EndOfStreamException)
                    {
                        // The stream has ended, end the input message loop
                        break;
                    }
                    catch (Exception e)
                    {
                        // Log the error and send an error event to the client
                        string message = string.Format("Exception occurred while receiving input message: {0}", e.Message);
                        Logger.Instance.Write(LogLevel.Error, message);

                        // TODO: Add event to output queue, and unit test it

                        // Continue the loop
                        continue;
                    }

                    // Verbose logging
                    string logMessage = string.Format("Received message of type[{0}] and method[{1}]",
                        incomingMessage.MessageType, incomingMessage.Method);
                    Logger.Instance.Write(LogLevel.Verbose, logMessage);

                    // Process the message
                    try
                    {
                        await DispatchMessage(incomingMessage);
                    }
                    catch (MethodHandlerDoesNotExistException)
                    {
                        // Method could not be handled, if the message was a request, send an error back to the client
                        // TODO: Localize
                        string mnfLogMessage = string.Format("Failed to find method handler for type[{0}] and method[{1}]",
                            incomingMessage.MessageType, incomingMessage.Method);
                        Logger.Instance.Write(LogLevel.Warning, mnfLogMessage);

                        if (incomingMessage.MessageType == MessageType.Request)
                        {
                            // TODO: Localize
                            Error mnfError = new Error {Code = -32601, Message = "Method not found"};
                            Message errorMessage = Message.CreateResponseError(incomingMessage.Id, mnfError);
                            outputQueue.Add(errorMessage, consumeInputCancellationToken);
                        }
                    }
                    catch (Exception e)
                    {
                        // General errors should be logged but not halt the processing loop
                        string geLogMessage = string.Format("Exception thrown when handling message of type[{0}] and method[{1}]: {2}",
                            incomingMessage.MessageType, incomingMessage.Method, e);
                        Logger.Instance.Write(LogLevel.Error, geLogMessage);
                        // TODO: Should we be returning a response for failing requests?
                    }
                }
                
                Logger.Instance.Write(LogLevel.Warning, "Exiting consume input loop!");
            }, consumeOutputCancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        internal Task ConsumeOutput()
        {
            return Task.Factory.StartNew(async () =>
            {
                while (!consumeOutputCancellationToken.IsCancellationRequested)
                {
                    Message outgoingMessage;
                    try
                    {
                        // Read message from the output queue
                        outgoingMessage = outputQueue.Take(consumeOutputCancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancelled during taking, end the loop
                        break;
                    }
                    catch (Exception e)
                    {
                        // If we hit an exception here, it is unrecoverable
                        string message = string.Format("Unexpected occurred while receiving output message: {0}", e.Message);
                        Logger.Instance.Write(LogLevel.Error, message);

                        break;
                    }

                    // Send the message 
                    string logMessage = string.Format("Sending message of type[{0}] and method[{1}]",
                        outgoingMessage.MessageType, outgoingMessage.Method);
                    Logger.Instance.Write(LogLevel.Verbose, logMessage);

                    await protocolChannel.MessageWriter.WriteMessage(outgoingMessage);
                }
                Logger.Instance.Write(LogLevel.Warning, "Exiting consume output loop!");
            }, consumeOutputCancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        internal async Task DispatchMessage(Message messageToDispatch)
        {
            Task handlerToAwait = null;

            switch (messageToDispatch.MessageType)
            {
                case MessageType.Request:
                    Func<Message, Task> requestHandler;
                    if (requestHandlers.TryGetValue(messageToDispatch.Method, out requestHandler))
                    {
                        handlerToAwait = requestHandler(messageToDispatch);
                    }
                    else
                    {
                       throw new MethodHandlerDoesNotExistException(MessageType.Request, messageToDispatch.Method);
                    }
                    break;
                case MessageType.Response:
                    TaskCompletionSource<Message> requestTask;
                    if (pendingRequests.TryRemove(messageToDispatch.Id, out requestTask))
                    {
                        requestTask.SetResult(messageToDispatch);
                        return;
                    }
                    else
                    {
                        throw new MethodHandlerDoesNotExistException(MessageType.Response, "response");
                    }
                case MessageType.Event:
                    Func<Message, Task> eventHandler;
                    if (eventHandlers.TryGetValue(messageToDispatch.Method, out eventHandler))
                    {
                        handlerToAwait = eventHandler(messageToDispatch);
                    }
                    else
                    {
                        throw new MethodHandlerDoesNotExistException(MessageType.Event, messageToDispatch.Method);
                    }
                    break;
                default:
                    // TODO: This case isn't handled properly
                    break;
            }

            // Skip processing if there isn't anything to do
            if (handlerToAwait == null)
            {
                return;
            }
            
            // Run the handler
            try
            {
                await handlerToAwait;
            }
            catch (TaskCanceledException)
            {
                // Some tasks may be cancelled due to legitimate
                // timeouts so don't let those exceptions go higher.
            }
            catch (AggregateException e)
            {
                if (!(e.InnerExceptions[0] is TaskCanceledException))
                {
                    // Cancelled tasks aren't a problem, so rethrow
                    // anything that isn't a TaskCanceledException
                    throw;
                }
            }
        }
        
        #endregion
    }
}
