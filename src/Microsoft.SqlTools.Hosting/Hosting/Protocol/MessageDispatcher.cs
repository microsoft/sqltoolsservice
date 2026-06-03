//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json.Linq;

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

        private SemaphoreSlim semaphore;

        private static long operationSequence;
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

        /// <summary>
        /// The maximum number of parallel operations that can be queued without blocking the main thread.
        /// Defaults to 100. This should be optimal to maintain a healthy application runtime state.
        /// If users need more parallel operations depending on if their systems support the same, they can always increase the limit.
        /// </summary>
        public int ParallelMessageProcessingLimit { get; set; } = 100;
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
            // Initialize semaphore for Parallel message processing using 10 initial requests.
            var initialSemaphoreLimit = ParallelMessageProcessingLimit <= 10 ? ParallelMessageProcessingLimit : 10;
            semaphore = new SemaphoreSlim(initialSemaphoreLimit, ParallelMessageProcessingLimit);

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
                    var operationStopwatch = Stopwatch.StartNew();
                    Logger.Verbose("Operation start");
                    Logger.Verbose($"Processing message with id[{requestMessage.Id}], of type[{requestMessage.MessageType}] and method[{requestMessage.Method}]");
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
                        Logger.Verbose($"Finished processing message with id[{requestMessage.Id}], of type[{requestMessage.MessageType}] and method[{requestMessage.Method}]");
                        operationStopwatch.Stop();
                        Logger.Verbose($"Operation finish durationMs:{operationStopwatch.ElapsedMilliseconds} status:success");
                    }
                    catch (Exception ex)
                    {
                        operationStopwatch.Stop();
                        Logger.Error($"Operation finish durationMs:{operationStopwatch.ElapsedMilliseconds} status:error");
                        Logger.Error($"{requestType.MethodName} : {ex.GetFullErrorMessage(true)}");
                        await requestContext.SendError(ex.GetFullErrorMessage());
                    }
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
                    var operationStopwatch = Stopwatch.StartNew();
                    Logger.Verbose("Operation start");
                    Logger.Verbose($"Processing message with id[{eventMessage.Id}], of type[{eventMessage.MessageType}] and method[{eventMessage.Method}]");
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
                        Logger.Verbose($"Finished processing message with id[{eventMessage.Id}], of type[{eventMessage.MessageType}] and method[{eventMessage.Method}]");
                        operationStopwatch.Stop();
                        Logger.Verbose($"Operation finish durationMs:{operationStopwatch.ElapsedMilliseconds} status:success");
                    }
                    catch (Exception ex)
                    {
                        operationStopwatch.Stop();
                        Logger.Error($"Operation finish durationMs:{operationStopwatch.ElapsedMilliseconds} status:error");
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
                    Logger.Error(message);
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
                    Logger.Error(message);
                    await MessageWriter.WriteEvent(HostingErrorEvent.Type, new HostingErrorParams { Message = message });

                    // Continue the loop
                    continue;
                }

                // The message could be null if there was an error parsing the
                // previous message.  In this case, do not try to dispatch it.
                if (newMessage != null)
                {
                    if (ShouldCreateOperationContext(newMessage))
                    {
                        using (Logger.BeginOperationScope(CreateOperationContext(newMessage)))
                        {
                            // Verbose logging
                            string logMessage =
                                $"Received message with id[{newMessage.Id}], of type[{newMessage.MessageType}] and method[{newMessage.Method}]";
                            Logger.Verbose(logMessage);

                            // Process the message
                            await this.DispatchMessage(newMessage, this.MessageWriter);
                        }
                    }
                    else
                    {
                        // Verbose logging
                        string logMessage =
                            $"Received message with id[{newMessage.Id}], of type[{newMessage.MessageType}] and method[{newMessage.Method}]";
                        Logger.Verbose(logMessage);

                        // Process the message
                        await this.DispatchMessage(newMessage, this.MessageWriter);
                    }
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
                var operationContext = Logger.CurrentOperationContext;
                IDisposable operationScope = null;
                if (operationContext == null && ShouldCreateOperationContext(messageToDispatch))
                {
                    operationContext = CreateOperationContext(messageToDispatch);
                    operationScope = Logger.BeginOperationScope(operationContext);
                }

                try
                {
                    if (this.ParallelMessageProcessing && isParallelProcessingSupported)
                    {
                        var capturedOperationContext = operationContext;
                        _ = Task.Run(async () =>
                        {
                            if (capturedOperationContext != null)
                            {
                                using (Logger.BeginOperationScope(capturedOperationContext))
                                {
                                    await handlerToAwait(messageToDispatch, messageWriter);
                                }
                            }
                            else
                            {
                                await handlerToAwait(messageToDispatch, messageWriter);
                            }
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
                    Logger.Verbose(string.Format("A TaskCanceledException occurred in the request handler: {0}", e.ToString()));
                }
                catch (Exception e)
                {
                    if (!(e is AggregateException exception && exception.InnerExceptions[0] is TaskCanceledException))
                    {
                        // Log the error but don't rethrow it to prevent any errors in the handler from crashing the service
                        Logger.Error(string.Format("An unexpected error occurred in the request handler: {0}", e.ToString()));
                    }
                }
                finally
                {
                    operationScope?.Dispose();
                }
            }
        }

        private static bool ShouldCreateOperationContext(Message message)
        {
            return message?.MessageType == MessageType.Request || message?.MessageType == MessageType.Event;
        }

        private static LogOperationContext CreateOperationContext(Message message)
        {
            return new LogOperationContext(
                operationId: CreateOperationId(),
                service: DeriveService(message.Method),
                rpcMethod: message.Method,
                rpcId: message.Id,
                rpcType: message.MessageType.ToString(),
                flowId: CreateFlowId(message.Contents));
        }

        private static string CreateOperationId()
        {
            var sequence = Interlocked.Increment(ref operationSequence);
            return string.Format(
                CultureInfo.InvariantCulture,
                "op-{0}-{1}",
                DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture),
                sequence);
        }

        private static string DeriveService(string method)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                return "serviceHost";
            }

            var normalized = method.ToLowerInvariant();
            if (normalized.StartsWith("connection/", StringComparison.Ordinal))
            {
                return "connection";
            }
            if (string.Equals(normalized, "query/syntaxparse", StringComparison.Ordinal))
            {
                return "languageService";
            }
            if (normalized.StartsWith("query/", StringComparison.Ordinal))
            {
                return "queryExecution";
            }
            if (normalized.StartsWith("queryexecutionplan/", StringComparison.Ordinal))
            {
                return "queryExecutionPlan";
            }
            if (IsWorkspaceDocumentMethod(normalized)
                || string.Equals(normalized, "workspace/didchangeconfiguration", StringComparison.Ordinal))
            {
                return "workspace";
            }
            if (normalized.StartsWith("textdocument/", StringComparison.Ordinal)
                || string.Equals(normalized, "workspace/symbol", StringComparison.Ordinal)
                || normalized.StartsWith("completion", StringComparison.Ordinal)
                || normalized.StartsWith("completionitem", StringComparison.Ordinal)
                || normalized.StartsWith("languageextension/", StringComparison.Ordinal)
                || normalized.StartsWith("sqltools/", StringComparison.Ordinal))
            {
                return "languageService";
            }
            if (normalized.StartsWith("objectexplorer/", StringComparison.Ordinal))
            {
                return "objectExplorer";
            }
            if (normalized.StartsWith("objectmanagement/", StringComparison.Ordinal))
            {
                return "objectManagement";
            }
            if (normalized.StartsWith("profiler/", StringComparison.Ordinal))
            {
                return "profiler";
            }
            if (normalized.StartsWith("schemacompare/", StringComparison.Ordinal))
            {
                return "schemaCompare";
            }
            if (normalized.StartsWith("schemadesigner/", StringComparison.Ordinal))
            {
                return "schemaDesigner";
            }
            if (normalized.StartsWith("tabledesigner/", StringComparison.Ordinal))
            {
                return "tableDesigner";
            }
            if (normalized.StartsWith("flatfile/", StringComparison.Ordinal))
            {
                return "flatFileImport";
            }
            if (normalized.StartsWith("filebrowser/", StringComparison.Ordinal))
            {
                return "fileBrowser";
            }
            if (normalized.StartsWith("cms/", StringComparison.Ordinal))
            {
                return "centralManagementServer";
            }
            if (normalized.StartsWith("account/", StringComparison.Ordinal))
            {
                return "authentication";
            }
            if (normalized.StartsWith("azurefunctions/", StringComparison.Ordinal))
            {
                return "azureFunctions";
            }
            if (normalized.StartsWith("notebookconvert/", StringComparison.Ordinal))
            {
                return "notebookConversion";
            }
            if (normalized.StartsWith("sqlpackage/", StringComparison.Ordinal))
            {
                return "sqlPackage";
            }
            if (normalized.StartsWith("dacfx/", StringComparison.Ordinal))
            {
                return "dacFx";
            }
            if (normalized.StartsWith("blob/", StringComparison.Ordinal))
            {
                return "azureBlob";
            }
            if (normalized.StartsWith("sqlprojects/", StringComparison.Ordinal))
            {
                return "sqlProjects";
            }
            if (normalized.StartsWith("admin/", StringComparison.Ordinal))
            {
                return "admin";
            }
            if (normalized.StartsWith("agent/", StringComparison.Ordinal))
            {
                return "agent";
            }
            if (normalized.StartsWith("assessment/", StringComparison.Ordinal))
            {
                return "assessment";
            }
            if (normalized.StartsWith("backup/", StringComparison.Ordinal))
            {
                return "backup";
            }
            if (normalized.StartsWith("restore/", StringComparison.Ordinal))
            {
                return "restore";
            }
            if (normalized.StartsWith("querystore/", StringComparison.Ordinal))
            {
                return "queryStore";
            }
            if (normalized.StartsWith("edit/", StringComparison.Ordinal))
            {
                return "editData";
            }
            if (normalized.StartsWith("metadata/", StringComparison.Ordinal))
            {
                return "metadata";
            }
            if (normalized.StartsWith("models/", StringComparison.Ordinal))
            {
                return "modelManagement";
            }
            if (normalized.StartsWith("scripting/", StringComparison.Ordinal))
            {
                return "scripting";
            }
            if (normalized.StartsWith("serialize/", StringComparison.Ordinal))
            {
                return "serialization";
            }
            if (normalized.StartsWith("tasks/", StringComparison.Ordinal))
            {
                return "tasks";
            }
            if (normalized.StartsWith("telemetry/", StringComparison.Ordinal))
            {
                return "telemetry";
            }
            if (string.Equals(normalized, "initialize", StringComparison.Ordinal)
                || string.Equals(normalized, "version", StringComparison.Ordinal)
                || string.Equals(normalized, "shutdown", StringComparison.Ordinal)
                || string.Equals(normalized, "exit", StringComparison.Ordinal)
                || normalized.StartsWith("hosting/", StringComparison.Ordinal)
                || normalized.StartsWith("capabilities/", StringComparison.Ordinal))
            {
                return "serviceHost";
            }

            return "serviceHost";
        }

        private static bool IsWorkspaceDocumentMethod(string normalizedMethod)
        {
            return string.Equals(normalizedMethod, "textdocument/didopen", StringComparison.Ordinal)
                || string.Equals(normalizedMethod, "textdocument/didchange", StringComparison.Ordinal)
                || string.Equals(normalizedMethod, "textdocument/didsave", StringComparison.Ordinal)
                || string.Equals(normalizedMethod, "textdocument/didclose", StringComparison.Ordinal);
        }

        private static string CreateFlowId(JToken contents)
        {
            var flowSource = FindFirstStringProperty(
                contents,
                "ownerUri",
                "OwnerUri",
                "uri",
                "Uri",
                "sessionId",
                "SessionId",
                "connectionId",
                "ConnectionId",
                "contextId",
                "ContextId");

            return string.IsNullOrWhiteSpace(flowSource) ? null : $"resource:{HashValue(flowSource)}";
        }

        private static string FindFirstStringProperty(JToken token, params string[] propertyNames)
        {
            if (token == null)
            {
                return null;
            }

            if (token is JObject obj)
            {
                foreach (var propertyName in propertyNames)
                {
                    if (obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out JToken value)
                        && value.Type != JTokenType.Null)
                    {
                        return value.Type == JTokenType.String ? value.Value<string>() : value.ToString();
                    }
                }

                foreach (var property in obj.Properties())
                {
                    var nested = FindFirstStringProperty(property.Value, propertyNames);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                {
                    var nested = FindFirstStringProperty(item, propertyNames);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }

            return null;
        }

        private static string HashValue(string value)
        {
#if NET6_0_OR_GREATER
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
#else
            byte[] hash;
            using (var sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            }
#endif
            return HashBytesToString(hash);
        }

        private static string HashBytesToString(byte[] hash)
        {
            var builder = new StringBuilder(16);
            for (int i = 0; i < 8 && i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
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

