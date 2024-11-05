//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SqlServer.SqlCopilot.Cartridges;
using Microsoft.SqlServer.SqlCopilot.Common;
using Microsoft.SqlTools.Connectors.VSCode;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Copilot.Contracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    public class CopilotConversation
    {
        public string ConversationUri { get; set; }

        public string CurrentMessage { get; set; }

        public SqlConnection SqlConnection { get; set; }

        public AutoResetEvent MessageCompleteEvent { get; set; }
    }

    public class CopilotConversationManager
    {
        //private readonly ActivitySource s_activitySource = new("SqlCopilot");
        //private static IChatCompletionService? _userChatCompletionService = null;
        //private ChatHistory? _chatHistory = null;
        //private static VSCodePromptExecutionSettings? _openAIPromptExecutionSettings = null;
        //private Kernel? _userSessionKernel = null;
        //private SqlExecAndParse? _sqlExecAndParseHelper = null;
        // private ExecutionAccessChecker? _accessChecker = null;
        // private static IProfile? _currentProfile = null;
        private static StringBuilder responseHistory = new StringBuilder();
        private static ConcurrentDictionary<string, CancellationTokenSource> _activeConversations = new ConcurrentDictionary<string, CancellationTokenSource>();

        private readonly ConcurrentDictionary<string, CopilotConversation> conversations = new();
        private readonly ChatMessageQueue messageQueue;
        private readonly Dictionary<string, SqlToolsRpcClient> rpcClients = new();
        private readonly ActivitySource activitySource = new("SqlCopilot");
        private Kernel userSessionKernel;
        private IChatCompletionService userChatCompletionService;
        private ChatHistory chatHistory;
        private static VSCodePromptExecutionSettings openAIPromptExecutionSettings;


        public CopilotConversationManager()
        {
            messageQueue = new ChatMessageQueue(conversations);
            InitializeTraceSource();
        }

        public LLMRequest RequestLLM(
            string conversationUri,
            IList<LanguageModelRequestMessage> messages,
            IList<LanguageModelChatTool> tools,
            AutoResetEvent responseReadyEvent)
        {
            return messageQueue.EnqueueRequest(
                conversationUri, messages, tools, responseReadyEvent);
        }

        public GetNextMessageResponse GetNextMessage(
            string conversationUri,
            string userText,
            LanguageModelChatTool tool,
            string toolParameters)
        {
            return messageQueue.ProcessNextMessage(
                conversationUri, userText, tool, toolParameters);
        }

        private void InitializeTraceSource()
        {
            // make sure any console listeners are configured to write to stderr so that the 
            // JSON RPC channel over stdio isn't clobbered.  If there is no console
            // listener then add one for stderr
            bool consoleListnerFound = false;
            foreach (TraceListener listener in SqlCopilotTrace.Source.Listeners)
            {
                if (listener is ConsoleTraceListener)
                {
                    // Change the ConsoleTraceListener to write to stderr instead of stdout
                    ((ConsoleTraceListener)listener).Writer = Console.Error;
                    consoleListnerFound = true;
                }
            }

            // if there is no console listener, add one for stderr
            if (!consoleListnerFound)
            {
                SqlCopilotTrace.Source.Listeners.Add(new ConsoleTraceListener() { Writer = Console.Error, TraceOutputOptions = TraceOptions.DateTime });
            }

            // Set trace level for the console output
            SqlCopilotTrace.Source.Switch.Level = SourceLevels.Information;
        }

        private void InitConversation(CopilotConversation conversation)
    {
        SqlCopilotTrace.WriteInfoEvent(SqlCopilotTraceEvents.ChatSessionStart, "Initializing Chat Session");

        try
        {
            // Create our services
            var sqlService = new SqlExecutionService(conversation.SqlConnection);
            var responseHandler = new ChatResponseHandler(conversation);
            var rpcClient = new SqlToolsRpcClient(sqlService, responseHandler);
            rpcClients[conversation.ConversationUri] = rpcClient;

            // Setup the kernel
            openAIPromptExecutionSettings = new VSCodePromptExecutionSettings
            {
                Temperature = 0.0,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                MaxTokens = 1500
            };

            var builder = Kernel.CreateBuilder();
            builder.AddVSCodeChatCompletion(new VSCodeLanguageModelEndpoint());

            userSessionKernel = builder.Build();
            activitySource.StartActivity("Main");

            // Setup access and tools
            var accessChecker = new ExecutionAccessChecker(userSessionKernel);
            var sqlExecHelper = new SqlExecAndParse(rpcClient, accessChecker);
            var currentDbConfig = SqlExecAndParse.GetCurrentDatabaseAndServerInfo().Result;

            // Add tools to kernel
            builder.Plugins.AddFromObject(sqlExecHelper);

            // Setup cartridges
            var services = new ServiceCollection();
            services.AddSingleton<SQLPluginHelperResourceServices>();
            
            var cartridgeBootstrapper = new Bootstrapper(rpcClient, accessChecker);
            cartridgeBootstrapper.LoadCartridges(
                services.BuildServiceProvider().GetRequiredService<SQLPluginHelperResourceServices>());
            cartridgeBootstrapper.LoadToolSets(builder, currentDbConfig);

            // Rebuild kernel with all plugins
            userSessionKernel = builder.Build();
            userChatCompletionService = userSessionKernel.GetRequiredService<IChatCompletionService>();

			// Initialize chat history
			var initialSystemMessage = @"System message: YOUR ROLE:
You are an AI copilot assistant running inside SQL Server Management Studio and connected to a specific SQL Server database.
Act as a SQL Server and SQL Server Management Studio SME.

GENERAL REQUIREMENTS:
- Work step-by-step, do not skip any requirements.
- **Important**: Do not re-call the same tool with identical parameters unless specifically prompted.
- If a tool has been successfully called, move on to the next step based on the user's query.
- Always confirm the schema of objects before assuming a default schema (e.g., dbo)";

            chatHistory = new ChatHistory(initialSystemMessage);
                
            SqlExecAndParse.SetAccessMode(CopilotAccessModes.READ_WRITE_NEVER);
            chatHistory.AddSystemMessage(
                $"Configuration information for currently connected database: {currentDbConfig}");

            // Wire up response handler events
            responseHandler.OnChatResponse += (e) =>
            {
                switch (e.UpdateType)
                {
                    case ResponseUpdateType.PartialResponseUpdate:
                        e.Conversation.CurrentMessage += e.PartialResponse;
                        break;
                    case ResponseUpdateType.Completed:
                        e.Conversation.MessageCompleteEvent.Set();
                        break;
                }
            };
        }
        catch (Exception e)
        {
            SqlCopilotTrace.WriteErrorEvent(
                SqlCopilotTraceEvents.ChatSessionFailed, 
                e.Message);
        }
    }

    public async Task<bool> StartConversation(string conversationUri, string connectionUri, string userText)
    {
        // Get DB connection
        DbConnection dbConnection = await ConnectionService.Instance.GetOrOpenConnection(
            connectionUri, ConnectionType.Default);
        
        if (!ConnectionService.Instance.TryGetAsSqlConnection(dbConnection, out var sqlConnection))
            return false;

        // Create and initialize conversation
        var conversation = new CopilotConversation
        {
            ConversationUri = conversationUri,
            SqlConnection = sqlConnection,
            CurrentMessage = string.Empty,
            MessageCompleteEvent = new AutoResetEvent(false)
        };

        InitConversation(conversation);
        conversations.AddOrUpdate(conversationUri, conversation, (_, _) => conversation);

        // Start processing in background
        _ = Task.Run(async () => 
            await SendUserPromptStreamingAsync(userText, conversationUri));

        return true;
    }



#if false
        public async Task<bool> StartConversation(string conversationUri, string connectionUri, string userText)
        {
            // get a connection to the database
            DbConnection dbConnection = await ConnectionService.Instance.GetOrOpenConnection(connectionUri, ConnectionType.Default);
            if (!ConnectionService.Instance.TryGetAsSqlConnection(dbConnection, out var sqlConnection))
            {
                return false;
            }

            CopilotConversation conversation = new CopilotConversation()
            {
                ConversationUri = conversationUri,
                SqlConnection = sqlConnection,
                CurrentMessage = string.Empty,
                MessageCompleteEvent = new AutoResetEvent(false)
            };

            InitConversation(conversation);

            conversations.AddOrUpdate(conversationUri, conversation, (key, oldValue) => conversation);

#pragma warning disable CS4014
            Task.Run(async () =>
            {
                _ = await SendUserPromptStreamingAsync(userText, conversationUri);
            });
#pragma warning restore CS4014

            return true;
        }
#endif
#if false
        private void InitConversation(CopilotConversation conversation)
        {
            SqlConnection sqlConnection = conversation.SqlConnection;
            SqlCopilotTrace.WriteInfoEvent(SqlCopilotTraceEvents.ChatSessionStart, "Initializing Chat Session");

            // Creating dependency injection container.
            var services = new ServiceCollection();
            services.AddSingleton<SQLPluginHelperResourceServices>();

            SqlCopilotTrace.WriteInfoEvent(SqlCopilotTraceEvents.ChatSessionStart, "Building kernel");
            try
            {

                // // setup execution settings
                _openAIPromptExecutionSettings = new VSCodePromptExecutionSettings();
                _openAIPromptExecutionSettings.Temperature = 0.0;
                _openAIPromptExecutionSettings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
                _openAIPromptExecutionSettings.MaxTokens = 1500;

                // setup the kernel builder
                var builder = Kernel.CreateBuilder();
                builder.AddVSCodeChatCompletion(new VSCodeLanguageModelEndpoint()).Build();

                // build the base user session kernel
                _userSessionKernel = builder.Build();
                s_activitySource.StartActivity("Main");

                sqlExecuteRpcParent = new SqlExecuteRpcParent(conversation);
                sqlExecuteRpcParent.OnChatResponse += OnChatResponse;

                // // get the version info of db/server to determine set of plugins to load
                _accessChecker = new ExecutionAccessChecker(_userSessionKernel);
                _sqlExecAndParseHelper = new SqlExecAndParse(sqlExecuteRpcParent, _accessChecker);
                var currentDbConfiguration = SqlExecAndParse.GetCurrentDatabaseAndServerInfo().Result;

                // load the bootstrapper functions
                _ = builder.Plugins.AddFromObject(_sqlExecAndParseHelper);

                // // load the cartridges and toolsets for the current database configuration
                Bootstrapper cartridgeBoostrapper = new Bootstrapper(sqlExecuteRpcParent, _accessChecker);
                cartridgeBoostrapper.LoadCartridges(services.BuildServiceProvider().GetRequiredService<SQLPluginHelperResourceServices>());
                cartridgeBoostrapper.LoadToolSets(builder, currentDbConfiguration);

                // // rebuild the kernel with the set of plugins for the current database
                _userSessionKernel = builder.Build();
                _userChatCompletionService = _userSessionKernel.GetRequiredService<IChatCompletionService>();

                // // Create chat history - initialize to default profile
                _currentProfile = cartridgeBoostrapper.CurrentProfile;
                //_chatHistory = new ChatHistory(_currentProfile!.DBA_ReadOnly().InitialPrompt());

                var initialSystemMessage = @"System message: YOUR ROLE:
You are an AI copilot assistant running inside SQL Server Management Studio and connected to a specific SQL Server database.
Act as a SQL Server and SQL Server Management Studio SME.

GENERAL REQUIREMENTS:
- Work step-by-step, do not skip any requirements.
- **Important**: Do not re-call the same tool with identical parameters unless specifically prompted.
- If a tool has been successfully called, move on to the next step based on the user's query.
- Always confirm the schema of objects before assuming a default schema (e.g., dbo)";

                _chatHistory = new ChatHistory(initialSystemMessage);
                
                SqlExecAndParse.SetAccessMode(CopilotAccessModes.READ_WRITE_NEVER);

                // add the current sql version and database name to the chat
                _chatHistory.AddSystemMessage($"Configuration information for currently connected database: {currentDbConfiguration}");
                return;
            }
            catch (Exception e)
            {
                // catch all exceptions, any failure means chat session won't work and error needs to 
                // get back to client.  Since JSON-RPC can't forward exceptions, log the error and return
                // an error code
                SqlCopilotTrace.WriteErrorEvent(SqlCopilotTraceEvents.ChatSessionFailed, e.Message);
                return;
            }
        }
#endif

        /// <summary>
        /// Event handler for the chat response received event from the SqlCopilotClient.
        /// This is then forwarded onto the SSMS specific events.  This is so in case the SQL 
        /// Copilot API changes, the impact is localized to this class.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChatResponse(object sender, ChatResponseEventArgs e)
        {
            ChatResponseEventArgs chatResponseArgs = new ChatResponseEventArgs();
            // forward the event onto the SSMS specific events the UI subscribes to
            switch (e.UpdateType)
            {
                case ResponseUpdateType.PartialResponseUpdate:
                    e.Conversation.CurrentMessage += e.PartialResponse;
                    break;

                case ResponseUpdateType.Completed:
                    e.Conversation.MessageCompleteEvent.Set();
                    break;

                case ResponseUpdateType.Started:
                    break;

                case ResponseUpdateType.Canceled:
                    break;
            }
        }

        public async Task<RpcResponse<string>> SendUserPromptStreamingAsync(string userPrompt, string chatExchangeId)
        {
            if (chatHistory == null)
            {
                var errorMessage = "Chat history not initialized.  Call InitializeAsync first.";
                SqlCopilotTrace.WriteErrorEvent(SqlCopilotTraceEvents.PromptReceived, errorMessage);
                return new RpcResponse<string>(SqlCopilotRpcReturnCodes.GeneralError, errorMessage);
            }

            if (userChatCompletionService == null) // || _plannerChatCompletionService == null || _databaseQueryChatCompletionService == null)
            {
                var errorMessage = "Chat completion service not initialized.  Call InitializeAsync first.";
                SqlCopilotTrace.WriteErrorEvent(SqlCopilotTraceEvents.PromptReceived, errorMessage);
                return new RpcResponse<string>(SqlCopilotRpcReturnCodes.GeneralError, errorMessage);
            }

            var sqlExecuteRpcParent = rpcClients[chatExchangeId];
            if (sqlExecuteRpcParent == null)
            {
                var errorMessage = "Communication channel not configured.  Call InitializeAsync first.";
                SqlCopilotTrace.WriteErrorEvent(SqlCopilotTraceEvents.PromptReceived, errorMessage);
                return new RpcResponse<string>(SqlCopilotRpcReturnCodes.GeneralError, errorMessage);
            }

            SqlCopilotTrace.WriteInfoEvent(SqlCopilotTraceEvents.PromptReceived, $"User prompt received.");
            SqlCopilotTrace.WriteVerboseEvent(SqlCopilotTraceEvents.PromptReceived, $"Prompt: {userPrompt}");

            // track this exchange and its cancellation token
            SqlCopilotTrace.WriteInfoEvent(SqlCopilotTraceEvents.PromptReceived, $"Tracking exchange {chatExchangeId}.");
            var cts = new CancellationTokenSource();
            _activeConversations.TryAdd(chatExchangeId, cts);

            // notify client of ID for this exchange
            await sqlExecuteRpcParent.InvokeAsync<string>("ChatExchangeStartedAsync", chatExchangeId);

            // setup the default response
            RpcResponse<string> promptResponse = new RpcResponse<string>("Success");

            try
            {
                chatHistory.AddUserMessage(userPrompt);
                StringBuilder completeResponse = new StringBuilder();

                var result = userChatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: userSessionKernel,
                    cts.Token);
                SqlCopilotTrace.WriteInfoEvent(SqlCopilotTraceEvents.PromptReceived, $"Prompt submitted to kernel.");

                // loop on the IAsyncEnumerable to get the results
                SqlCopilotTrace.WriteVerboseEvent(SqlCopilotTraceEvents.ResponseGenerated, "Response:");
                for (int awaitAttempts = 0; awaitAttempts < 10; awaitAttempts++)
                {
                    try
                    {
                        await foreach (var content in result)
                        {
                            if (!string.IsNullOrEmpty(content.Content))
                            {
                                SqlCopilotTrace.WriteVerboseEvent(SqlCopilotTraceEvents.ResponseGenerated, content.Content);
                                completeResponse.Append(content.Content);
                                await sqlExecuteRpcParent.InvokeAsync<string>("HandlePartialResponseAsync", chatExchangeId, content.Content);
                            }
                        }

                        if (completeResponse.Length > 0)
                        {
                            // got the response, break out of the retry loop
                            break;
                        }
                    }
                    catch (HttpOperationException e) when (e.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        // system is outpacing the OpenAI endpoint.  Wait for 30s and try again
                        SqlCopilotTrace.WriteErrorEvent(SqlCopilotTraceEvents.CopilotServerException, $"API rate limit exceeded.  Backing off for 30 seconds.");
                        await Task.Delay(30000);
                        continue;
                    }
                    catch (HttpOperationException e) when (e.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        string exceptionMessage = e.Message;
                        if (e.ResponseContent != null)
                        {
                            // Extract the Response Content that contains the specific filtering errors
                            var exceptionContent = JObject.Parse(e.ResponseContent);

                            // Check for ResponsibleAIPolicyViolation in innererror code and extract the content filter result
                            var innerError = exceptionContent["error"]?["innererror"];
                            if (innerError?["code"]?.ToString() == "ResponsibleAIPolicyViolation")
                            {
                                // Contains content filter results with categories such as hate, jailbreak, self-harm, sexual, and violence.
                                // Find the first filtered violation and extract the name of the filtering content error
                                var contentFilterResult = innerError["content_filter_result"];
                                var filteredItem = contentFilterResult?.FirstOrDefault(filter => filter.First?["filtered"]?.ToObject<bool>() == true);
                                var filteredMessage = filteredItem != null ? String.Format(CommonResources.ContentFiltering, ((JProperty)filteredItem).Name.ToString()) : null;
                                exceptionMessage = filteredMessage != null ? filteredMessage : exceptionMessage;
                            }
                        }
                        SqlCopilotTrace.WriteErrorEvent(SqlCopilotTraceEvents.CopilotServerException, exceptionMessage);
                        return new RpcResponse<string>(SqlCopilotRpcReturnCodes.ApiException, exceptionMessage);
                    }
                    catch (HttpOperationException)
                    {
                        throw;
                    }
                }

                if (cts.IsCancellationRequested)
                {
                    // client cancelled the request
                    SqlCopilotTrace.WriteInfoEvent(SqlCopilotTraceEvents.PromptReceived, $"Client canceled exchange {chatExchangeId}.");
                    await sqlExecuteRpcParent.InvokeAsync<string>("ChatExchangeCanceledAsync", chatExchangeId);

                    // change the prompt response to indicate the cancellation
                    promptResponse = new RpcResponse<string>(SqlCopilotRpcReturnCodes.TaskCancelled, "Request canceled by client.");
                }
                else
                {
                    // add the assistant response to the chat history
                    SqlCopilotTrace.WriteInfoEvent(SqlCopilotTraceEvents.ResponseGenerated, "Response completed. Updating chat history.");
                    await sqlExecuteRpcParent.InvokeAsync<string>("ChatExchangeCompleteAsync", chatExchangeId);
                    chatHistory.AddAssistantMessage(completeResponse.ToString());
                    responseHistory.Append(completeResponse);
                }
            }
            catch (HttpOperationException e)
            {
                SqlCopilotTrace.WriteErrorEvent(SqlCopilotTraceEvents.CopilotServerException, e.Message);
                return new RpcResponse<string>(SqlCopilotRpcReturnCodes.ApiException, e.Message);
            }
            finally
            {

                // remove this exchange ID from the active conversations
                SqlCopilotTrace.WriteInfoEvent(SqlCopilotTraceEvents.PromptReceived, $"Removing exchange {chatExchangeId} from active conversations.");
                _activeConversations.Remove(chatExchangeId, out cts);
            }

            return promptResponse;
        }
    }

    /// <summary>
    /// event data for a processing update message
    /// </summary>
    public class ProcessingUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// the unique ID of the chat exchange this completion event is for
        /// </summary>
        public string ChatExchangeId { get; set; }

        /// <summary>
        /// The type of processing update message
        /// </summary>
        public ProcessingUpdateType UpdateType { get; set; }

        /// <summary>
        /// The details for the processing update.  Callers should be resilient to extra parameters being added in the future or not always being present.
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }
    }

    /// <summary>
    /// the type of procsessing update the event is for
    /// </summary>
    public enum ResponseUpdateType
    {
        /// <summary>
        /// processing of the prompt has started
        /// </summary>
        Started,

        /// <summary>
        /// processing of the prompt has completed
        /// </summary>
        Completed,

        /// <summary>
        /// a partial response has been generated (streaming)
        /// </summary>
        PartialResponseUpdate,

        /// <summary>
        /// the prompt processing has been canceled (user initiated)
        /// </summary>
        Canceled
    }

    /// <summary>
    /// common event data for chat response events
    /// </summary>
    public class ChatResponseEventArgs : EventArgs
    {
        /// <summary>
        /// type of processing update event
        /// </summary>
        public ResponseUpdateType UpdateType { get; set; }

        /// <summary>
        /// the unique ID of the chat exchange this completion event is for
        /// </summary>
        public string ChatExchangeId { get; set; }

        /// <summary>
        /// the partial response if it is a partial response event
        /// </summary>
        public string PartialResponse { get; set; }

        public CopilotConversation Conversation { get; set; }
    }

#if false
    /// <summary>
    /// concrete implementation of the IJsonRpcParent interface for RPC calls to the parent process
    /// </summary>
    public class SqlExecuteRpcParent : IJsonRpcParent
    {
        private SqlConnection _sqlConnection;
        private CopilotConversation conversation;
    
        /// <summary>
        /// constructor
        /// </summary>
        public SqlExecuteRpcParent(CopilotConversation conversation)
        {
            this.conversation = conversation;
            this._sqlConnection = conversation.SqlConnection;
        }

        /// <summary>
        /// abstraction of the JSON-RPC call to the invoke a method asynchronously
        /// </summary>
        /// <typeparam name="T">return type</typeparam>
        /// <param name="method">method to invoke</param>
        /// <param name="parameters">params to method</param>
        /// <returns></returns>
        public async Task<T> InvokeAsync<T>(string method, params object[] parameters)
        {
            if (method == "ExecuteSqlQueryAsync")
            {
                if (parameters[2] == Array.Empty<string>() || parameters[2] == Array.Empty<object>())
                {
                    object execResults = await ExecuteSqlQueryAsync((string)parameters[0], (bool)parameters[1]);
                    return (typeof(T) == typeof(string)) ? (T)execResults : throw new Exception("Invalid return type");
                }
                else
                {
                    object execResults = await ExecuteSqlQueryAsync((string)parameters[0], (bool)parameters[1], (string[])parameters[2]);
                    return (typeof(T) == typeof(string)) ? (T)execResults : throw new Exception("Invalid return type");
                }
            }
            else if (method == "QueryUserDatabaseAsync")
            {
                //return QueryUserDatabaseAsync<T>((string)parameters[0], (string)parameters[1]);
                return default;
            }
            else if (method == "PromptProcessingUpdate")
            {
                return typeof(T) == typeof(string) ?
                    (T)(object)await PromptProcessingUpdate((string)parameters[0], (ProcessingUpdateType)parameters[1], (Dictionary<string, string>)parameters[2]) :
                    throw new Exception("Invalid return type");
                //return PromptProcessingUpdate<T>((string)parameters[0], (ProcessingUpdateType)parameters[1], (Dictionary<string, string>)parameters[2]);
            }
            else if (method == "ChatExchangeStartedAsync")
            {
                await ChatExchangeStartedAsync((string)parameters[0]);
                return default;
            }
            else if (method == "ChatExchangeCompleteAsync")
            {
                await ChatExchangeCompleteAsync((string)parameters[0]);
                return default;
            }
            else if (method == "ChatExchangeCanceledAsync")
            {
                await ChatExchangeCanceledAsync((string)parameters[0]);
                return default;
            }
            else if (method == "HandlePartialResponseAsync")
            {
                await HandlePartialResponseAsync((string)parameters[0], (string)parameters[1]);
                return default;
            }
            else
            {
                throw new NotImplementedException("Method not implemented: " + method);
            }
        }

        /// <summary>
        /// Implement ExecuteQueryAsync.  This is used by the 'server' as a callback mechanism for native functions
        /// to be able to execute a query in the context of the client-side application
        /// </summary>
        /// <param name="query"></param>
        /// <param name="execSP"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<string> ExecuteSqlQueryAsync(string query, bool execSP, params object[] parameters)
        {
            if (_sqlConnection != null && _sqlConnection.State == ConnectionState.Open)
            {
                // create a command to execute the sql
                var command = new SqlCommand(query, _sqlConnection);

                // add the parameters to the command
                foreach (var param in parameters)
                {
                    var paramStr = param.ToString();
                    var paramParts = paramStr.Split('=');
                    _ = command.Parameters.AddWithValue(paramParts[0], paramParts[1]);
                }

                // if it is a stored procedure, set the command type to stored procedure
                if (execSP)
                {
                    command.CommandType = CommandType.StoredProcedure;
                }

                try
                {
                    // execute the query async and then process the results, returning them as a string
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var result = new StringBuilder();
                        do
                        {
                            while (reader.Read())
                            {
                                for (var i = 0; i < reader.FieldCount; i++)
                                {
                                    _ = result.Append(reader.GetName(i) + ": " + reader.GetValue(i) + "\n");
                                }
                            }
                            _ = result.Append("\n");
                        } while (reader.NextResult());

                        return result.ToString();
                    }
                }
                catch (Exception e)
                {
                    // return the error to the LLM
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    return $"The following error occured querying the database: {e.Message}";
                }
            }
            else
            {
                return "Not connected to a database";
            }
        }

        /// <summary>
        /// Raise the OnChatResponseResponseReceived event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void RaiseOnChatResponse(ChatResponseEventArgs e)
        {
            OnChatResponse?.Invoke(this, e);
        }

        /// <summary>
        /// Raise the OnProcessingupdate event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void RaiseOnProcessingUpdate(ProcessingUpdateEventArgs e)
        {
            OnProcessingUpdate?.Invoke(this, e);
        }

        /// <summary>
        /// Handle a partial response from the chat service.  The chat service streams responses back to the client.
        /// </summary>
        /// <param name="chatExchangeId"></param>
        /// <param name="partialResponse"></param>
        /// <returns></returns>
        public async Task HandlePartialResponseAsync(string chatExchangeId, string partialResponse)
        {
            await Task.Run(() =>
            {
                RaiseOnChatResponse(new ChatResponseEventArgs
                {
                    UpdateType = ResponseUpdateType.PartialResponseUpdate,
                    ChatExchangeId = chatExchangeId,
                    PartialResponse = partialResponse,
                    Conversation = conversation
                });
            });
        }

        /// <summary>
        /// Chat response streaming has started.  This method provides the unique exchange ID for the submitted prompt.
        /// </summary>
        /// <param name="chatExchangeId"></param>
        /// <returns></returns>
        public async Task ChatExchangeStartedAsync(string chatExchangeId)
        {
            await Task.Run(() =>
            {
                RaiseOnChatResponse(new ChatResponseEventArgs
                {
                    UpdateType = ResponseUpdateType.Started,
                    ChatExchangeId = chatExchangeId,
                    Conversation = conversation
                });
            });
        }

        /// <summary>
        /// Complete a chat response. This will terminate the exchange.  The server is ready to accept a new prompt.
        /// </summary>
        /// <param name="chatExchangeId"></param>
        /// <returns></returns>
        public async Task ChatExchangeCompleteAsync(string chatExchangeId)
        {
            await Task.Run(() =>
            {
                RaiseOnChatResponse(new ChatResponseEventArgs
                {
                    UpdateType = ResponseUpdateType.Completed,
                    ChatExchangeId = chatExchangeId,
                    Conversation = conversation
                });
            });
        }

        /// <summary>
        /// Complete a chat response. This will terminate the exchange.  The server is ready to accept a new prompt.
        /// </summary>
        /// <param name="chatExchangeId"></param>
        /// <returns></returns>
        public async Task ChatExchangeCanceledAsync(string chatExchangeId)
        {
            await Task.Run(() =>
            {
                RaiseOnChatResponse(new ChatResponseEventArgs
                {
                    UpdateType = ResponseUpdateType.Canceled,
                    ChatExchangeId = chatExchangeId,
                    Conversation = conversation
                });
            });
        }

        /// <summary>
        /// Event for receiving processing updates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ProcessingUpdate(object sender, ProcessingUpdateEventArgs e);

        /// <summary>
        /// Subscribable event for receiving a chat completed event
        /// </summary>
        public event ProcessingUpdate OnProcessingUpdate;

        /// <summary>
        /// Subscribable event for receiving a partial chat response
        /// </summary>
        public event ChatResponse OnChatResponse;

        /// <summary>
        /// Event for receiving a complete chat response
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ChatResponse(object sender, ChatResponseEventArgs e);

        

        /// <summary>
        /// An update message from the server process analyzing the prompt.  The update includes a message type and a variable set of parameters.
        /// Consumers of the parameters should be resilient to extra parameters being added in the future or not always being present.
        /// </summary>
        /// <param name="chatExchangeId">The exchange id this update message is a part of</param>
        /// <param name="updateType">The type of update message</param>
        /// <param name="parameters">key/value dictionary of parameters for the update message</param>
        /// <returns></returns>
        public async Task<string> PromptProcessingUpdate(string chatExchangeId, ProcessingUpdateType updateType, Dictionary<string, string> parameters)
        {
            await Task.Run(() =>
            {
                RaiseOnProcessingUpdate(new ProcessingUpdateEventArgs
                {
                    ChatExchangeId = chatExchangeId,
                    UpdateType = updateType,
                    Parameters = parameters
                });
            });
            return "success";
        }
    }
#endif
}
