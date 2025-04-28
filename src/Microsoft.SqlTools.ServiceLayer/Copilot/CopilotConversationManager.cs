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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Scriptoria.Common;
using Microsoft.Scriptoria.Interfaces;
using Microsoft.Scriptoria.Models;
using Microsoft.Scriptoria.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SqlServer.SqlCopilot.Common;
using Microsoft.SqlServer.SqlCopilot.SqlScriptoria;
using Microsoft.SqlServer.SqlCopilot.SqlScriptoriaCommon;
using Microsoft.SqlTools.Connectors.VSCode;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Copilot.Contracts;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json;
// using static Microsoft.Scriptoria.Interfaces.IKernelBuilderFactory;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    public class ConversationState
    {
        public string Response { get; set; }
        public LanguageModelChatTool ResponseTool { get; set; }
        public string ResponseToolParameters { get; set; }
    }

    public class CopilotConversation
    {
        public string ConversationUri { get; set; }
        public string CurrentMessage { get; set; }
        public SqlConnection SqlConnection { get; set; }
        public ConversationState State { get; set; }
        public TaskCompletionSource<ConversationState> CompletionSource { get; set; } = new TaskCompletionSource<ConversationState>();
    }

    //public class KernelBuilderFactory : IKernelBuilderFactory
    //{
    //    private readonly CopilotConversation conversation;

    //    public KernelBuilderFactory(CopilotConversation conversation)
    //    {
    //        this.conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
    //    }

    //    public IKernelBuilder Create(KernelType kernelType)
    //    {
    //        var builder = Kernel.CreateBuilder();

    //        switch (kernelType)
    //        {
    //            case KernelType.Default:
    //                builder.AddVSCodeChatCompletion(new VSCodeLanguageModelEndpoint(
    //                    conversation,
    //                    RequestMessageType.ToolCallRequest));
    //                break;

    //            case KernelType.Minion:
    //                builder.AddVSCodeChatCompletion(new VSCodeLanguageModelEndpoint(
    //                    conversation,
    //                    RequestMessageType.DirectRequest));
    //                break;

    //            default:
    //                throw new ArgumentOutOfRangeException(nameof(kernelType), kernelType, "Invalid kernel type.");
    //        }

    //        return builder;
    //    }
    //}

    public class CopilotConversationManager
    {
        private static StringBuilder responseHistory = new StringBuilder();
        private static ConcurrentDictionary<string, CancellationTokenSource> activeConversations = new ConcurrentDictionary<string, CancellationTokenSource>();
        private readonly ConcurrentDictionary<string, CopilotConversation> conversations = new();
        private readonly ChatMessageQueue messageQueue;
        private readonly Dictionary<string, SqlToolsRpcClient> rpcClients = new();
        private readonly ActivitySource activitySource = new("SqlCopilot");
        private Kernel userSessionKernel;
        private IChatCompletionService userChatCompletionService;
        private ChatHistory chatHistory;
        private static VSCodePromptExecutionSettings openAIPromptExecutionSettings;

        private static IDictionary<string, string> _connectionContext = new Dictionary<string, string>();

        // active cartridge
        private static CartridgeBase? _activeCartridge = null;
        // private static ChatHistory? _chatHistory = null;

        public CopilotConversationManager()
        {
            messageQueue = new ChatMessageQueue(conversations);
        }

        public async Task<bool> StartConversation(string conversationUri, string connectionUri, string userText)
        {
            Logger.Verbose($"Start Copilot conversation '{conversationUri}' for connection '{connectionUri}'");

            if (!ConnectionService.Instance.OwnerToConnectionMap.TryGetValue(
                connectionUri, out ConnectionInfo connectionInfo))
            {
                return false;
            }

            // Get DB connection
            DbConnection dbConnection = await ConnectionService.Instance.GetOrOpenConnection(
                connectionUri, ConnectionType.Default);

            if (!ConnectionService.Instance.TryGetAsSqlConnection(dbConnection, out var sqlConnection))
            {
                Logger.Verbose($"Could not get connection '{connectionUri}'");
                return false;
            }

            // Create and initialize conversation
            var conversation = new CopilotConversation
            {
                ConversationUri = conversationUri,
                SqlConnection = sqlConnection,
                CurrentMessage = string.Empty,
            };

            await InitConversation(conversation);
            conversations.AddOrUpdate(conversationUri, conversation, (_, _) => conversation);

            // Start processing in background
            _ = Task.Run(async () =>
                await SendUserPromptStreamingAsync(userText, conversationUri));

            return true;
        }

        public void AddOrUpdateConversation(string conversationUri, CopilotConversation conversation)
        {
            conversations.AddOrUpdate(conversationUri, conversation, (_, _) => conversation);
        }

        public async Task<ChatMessage> QueueLLMRequest(
            string conversationUri,
            RequestMessageType type,
            IList<LanguageModelRequestMessage> messages,
            IList<LanguageModelChatTool> tools)
        {
            conversations.TryGetValue(conversationUri, out var conversation);
            var chatRequest = new ChatMessage(type, conversationUri, messages, tools, conversation);
            await messageQueue.EnqueueMessageAsync(chatRequest);
            return chatRequest;
        }

        public async Task<GetNextMessageResponse> GetNextMessage(
            string conversationUri,
            string userText,
            LanguageModelChatTool tool,
            string toolParameters)
        {
            return await messageQueue.ProcessNextMessage(
                conversationUri, userText, tool, toolParameters);
        }

        private async Task InitConversation(CopilotConversation conversation)
        {
            Logger.Verbose("Initializing Chat Session");

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
                builder.AddVSCodeChatCompletion(new VSCodeLanguageModelEndpoint(conversation, RequestMessageType.ToolCallRequest));
                userSessionKernel = builder.Build();

                //var kernelBuilderFactory = new KernelBuilderFactory(conversation);
                //var builder = kernelBuilderFactory.Create(KernelType.Default);
                //userSessionKernel = builder.Build();

                activitySource.StartActivity("Main");

                // Setup cartridges
                var services = new ServiceCollection();

                // Register the ScriptoriaTrace as IScriptoriaTrace
                services.AddSingleton<IScriptoriaTrace, CopilotLogger>();
                services.AddSingleton<ICartridgeContentManager, CartridgeContentManager>();
                services.AddSingleton<ICartridgeDataAccess>((sp) => sqlService!);
                services.AddSingleton<ICartridgeListener>((sp) => sqlService!);
                services.AddSingleton<IScriptoriaExecutionContext, SqlScriptoriaExecutionContext>();
                services.AddSingleton<IExecutionAccessCheckerFactory, ChatExecutionAccessCheckerFactory>();
                services.AddSingleton<IKernelBuilder>((sp) => builder);

                ContentProviderType contentLibraryProviderType = ContentProviderType.Resource;

                // initialize the bootstrapper that will discover and find the correct cartridge to use for the current 
                // connection context
                var contentManager = services.BuildServiceProvider().GetRequiredService<ICartridgeContentManager>();
                contentManager.ContentProviderType = contentLibraryProviderType;

                Logger.Verbose($"SqlScriptoria version: {contentManager.Version}");

                IServiceProvider serviceProvider = services.BuildServiceProvider();

                Bootstrapper cartridgeBootstrapper = new Bootstrapper(serviceProvider, new ChatExecutionAccessCheckerFactory());

                var _executionContext = serviceProvider.GetRequiredService<IScriptoriaExecutionContext>();

                // we need the current connection context to be able to load the cartridges.  To the connection 
                // context we add what experience this is being loaded into by the client.
                await _executionContext.LoadExecutionContextAsync(CartridgeExperienceKeyNames.VSCode_MSSQL_TsqlEditorChat, sqlService!);


                // the active cartridge (there can only be one in the current design) is loaded using the current db config 
                // the 'Experience' and 'Version' properties will deterime which cartridge is loaded.
                // other configuration values will be used by toolsets in the future as well.
                _activeCartridge = cartridgeBootstrapper.LoadCartridge();
                await _activeCartridge.InitializeToolsetsAsync();

                // rebuild the kernel with the set of plugins for the current database
                userSessionKernel = builder.Build();
                userChatCompletionService = userSessionKernel.GetRequiredService<IChatCompletionService>();

                // read-only is the default startup mode
                _activeCartridge.AccessChecker.ExecutionMode = CopilotAccessModes.READ_WRITE_NEVER;

                // InitializeChatHistory();


                // Initialize chat history
                var initialSystemMessage = @"System message: YOUR ROLE:
You are an AI copilot assistant running inside Visual Studio Code and connected to a specific SQL Server database.
Act as a SQL Server and VS Code SME.

GENERAL REQUIREMENTS:
- Work step-by-step, do not skip any requirements.
- **Important**: Do not assume a default schema (e.g. dbo) for database objects when calling tool functions.  If an table does not contain a scehema call the GetTableNames tool lookup the schema before calling other tools with the object name.  This is IMPORTANT or other tools will fail.
- **Important**: Do not re-call the same tool with identical parameters unless specifically prompted.
- If a tool has been successfully called, move on to the next step based on the user's query.";

                chatHistory = new ChatHistory(initialSystemMessage);
                var connectionContextString = JsonConvert.SerializeObject(_executionContext.ContextSettings);
                chatHistory.AddSystemMessage(
                    $"Configuration information for currently connected database: {connectionContextString}");

                // Wire up response handler events
                responseHandler.OnChatResponse += async (e) =>
                {
                    switch (e.UpdateType)
                    {
                        case ResponseUpdateType.PartialResponseUpdate:
                            e.Conversation.CurrentMessage += e.PartialResponse;
                            break;
                        case ResponseUpdateType.Completed:
                            await messageQueue.EnqueueMessageAsync(new ChatMessage(
                                RequestMessageType.Response, conversation.ConversationUri, null, null, conversation));
                            break;
                    }
                };
            }
            catch (Exception e)
            {
                Logger.Error($"ChatSessionFailed: {e.Message}");
            }
        }

        /// <summary>
        /// Event handler for the chat response received event from the SqlCopilotClient.
        /// This is then forwarded onto the SSMS specific events.  This is so in case the SQL 
        /// Copilot API changes, the impact is localized to this class.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task OnChatResponse(object sender, ChatResponseEventArgs e)
        {
            ChatResponseEventArgs chatResponseArgs = new ChatResponseEventArgs();
            // forward the event onto the SSMS specific events the UI subscribes to
            switch (e.UpdateType)
            {
                case ResponseUpdateType.PartialResponseUpdate:
                    e.Conversation.CurrentMessage += e.PartialResponse;
                    break;

                case ResponseUpdateType.Completed:
                    await messageQueue.EnqueueMessageAsync(new ChatMessage(
                                RequestMessageType.Response, e.Conversation.ConversationUri, null, null, e.Conversation));
                    break;

                case ResponseUpdateType.Started:
                    break;

                case ResponseUpdateType.Canceled:
                    break;
            }
        }

        private async Task<RpcResponse<string>> SendUserPromptStreamingAsync(string userPrompt, string chatExchangeId)
        {
            if (chatHistory == null)
            {
                var errorMessage = "Chat history not initialized.  Call InitializeAsync first.";
                Logger.Error($"Prompt Received Error: {errorMessage}");
                return new RpcResponse<string>(SqlCopilotRpcReturnCodes.GeneralError, errorMessage);
            }

            if (userChatCompletionService == null) // || _plannerChatCompletionService == null || _databaseQueryChatCompletionService == null)
            {
                var errorMessage = "Chat completion service not initialized.  Call InitializeAsync first.";
                Logger.Error($"Prompt Received Error: {errorMessage}");
                return new RpcResponse<string>(SqlCopilotRpcReturnCodes.GeneralError, errorMessage);
            }

            var sqlExecuteRpcParent = rpcClients[chatExchangeId];
            if (sqlExecuteRpcParent == null)
            {
                var errorMessage = "Communication channel not configured.  Call InitializeAsync first.";
                Logger.Error($"Prompt Received Error: {errorMessage}");
                return new RpcResponse<string>(SqlCopilotRpcReturnCodes.GeneralError, errorMessage);
            }

            Logger.Verbose( $"User prompt received.");
            Logger.Verbose($"Prompt: {userPrompt}");

            // track this exchange and its cancellation token
            Logger.Verbose($"Tracking exchange {chatExchangeId}.");
            var cts = new CancellationTokenSource();
            activeConversations.TryAdd(chatExchangeId, cts);

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
                Logger.Verbose( $"Prompt submitted to kernel.");

                // loop on the IAsyncEnumerable to get the results
                Logger.Verbose("Response Generated:");
                for (int awaitAttempts = 0; awaitAttempts < 10; awaitAttempts++)
                {
                    await foreach (var content in result)
                    {
                        if (!string.IsNullOrEmpty(content.Content))
                        {
                            Logger.Verbose(content.Content);
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

                if (cts.IsCancellationRequested)
                {
                    // client cancelled the request
                    Logger.Verbose($"Client canceled exchange {chatExchangeId}.");
                    await sqlExecuteRpcParent.InvokeAsync<string>("ChatExchangeCanceledAsync", chatExchangeId);

                    // change the prompt response to indicate the cancellation
                    promptResponse = new RpcResponse<string>(SqlCopilotRpcReturnCodes.TaskCancelled, "Request canceled by client.");
                }
                else
                {
                    // add the assistant response to the chat history
                    Logger.Verbose("Response completed. Updating chat history.");
                    await sqlExecuteRpcParent.InvokeAsync<string>("ChatExchangeCompleteAsync", chatExchangeId);
                    chatHistory.AddAssistantMessage(completeResponse.ToString());
                    responseHistory.Append(completeResponse);
                }
            }
            catch (HttpOperationException e)
            {
                Logger.Error($"Copilot Server Exception: {e.Message}");
                return new RpcResponse<string>(SqlCopilotRpcReturnCodes.ApiException, e.Message);
            }
            finally
            {

                // remove this exchange ID from the active conversations
                Logger.Verbose( $"Removing exchange {chatExchangeId} from active conversations.");
                activeConversations.Remove(chatExchangeId, out cts);
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

        /// <summary>
        /// the conversation this response is for
        /// </summary>
        public CopilotConversation Conversation { get; set; }
    }
}
