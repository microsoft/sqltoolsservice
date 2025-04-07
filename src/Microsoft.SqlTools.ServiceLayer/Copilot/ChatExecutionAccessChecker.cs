//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// using Microsoft.Extensions.DependencyInjection;
using Microsoft.Scriptoria.Common;
using Microsoft.Scriptoria.Interfaces;
using Microsoft.Scriptoria.Services;
using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.ChatCompletion;
//using Microsoft.SqlServer.SqlCopilot.SqlScriptoria;
//using Microsoft.SqlServer.SqlCopilot.SqlScriptoriaCommon;
//using Microsoft.SqlTools.Connectors.VSCode;
using Microsoft.SqlTools.ServiceLayer.Copilot.Contracts;
//using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    /// <summary>
    /// A factory for creating instances of <see cref="ExecutionAccessChecker"/>.
    /// </summary>
    public class ChatExecutionAccessCheckerFactory : IExecutionAccessCheckerFactory
    {
        /// <inheritdoc />
        public IExecutionAccessChecker Create(Kernel kernel, IList<string> readOnlyProcs, IScriptoriaTrace scriptoriaTrace)
        {
            // Creates and returns a new ExecutionAccessChecker.
            return new ChatExecutionAccessChecker(readOnlyProcs, scriptoriaTrace);
        }
    }


#if false
    public interface IKernelFactory
    {
        Kernel Create();
    }


    /// <summary>
    /// Factory for creating and configuring <see cref="Kernel"/> instances.
    /// </summary>
    public class ChatKernelFactory : IKernelFactory
    {
        private readonly CopilotConversation _conversation;
        private readonly ServiceCollection _services;

        public ChatKernelFactory(CopilotConversation conversation)
        {
            _conversation = conversation;
            _services = new ServiceCollection();
        }

        /// <inheritdoc />
        public Kernel Create()
        {
            Logger.Verbose("Initializing Chat Session Kernel");

            // Initialize services
            var sqlService = new SqlExecutionService(_conversation.SqlConnection);
            var responseHandler = new ChatResponseHandler(_conversation);
            var rpcClient = new SqlToolsRpcClient(sqlService, responseHandler);

            // Configure OpenAI execution settings
            var openAIPromptExecutionSettings = new VSCodePromptExecutionSettings
            {
                Temperature = 0.0,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                MaxTokens = 1500
            };

            // Create the kernel
            var builder = Kernel.CreateBuilder();
            builder.AddVSCodeChatCompletion(new VSCodeLanguageModelEndpoint(_conversation));

            var kernel = builder.Build();

            // Setup Cartridge system
            _services.AddSingleton<CartridgeContentManager>();
            _services.AddSingleton<CopilotLogger>();
            var contentManager = _services.BuildServiceProvider().GetRequiredService<CartridgeContentManager>();
            contentManager.ContentProviderType = ContentProviderType.Resource;

            Logger.Verbose($"SqlScriptoria version: {contentManager.Version}");


            IServiceProvider serviceProvider = _services.BuildServiceProvider();

            var cartridgeBootstrapper = new Bootstrapper(
                serviceProvider,
                new ChatExecutionAccessCheckerFactory()
            );


            IScriptoriaTrace copilotLogger = serviceProvider.GetRequiredService<IScriptoriaTrace>();

            SqlScriptoriaExecutionContext _executionContext = new(copilotLogger);

            // we need the current connection context to be able to load the cartridges.  To the connection 
            // context we add what experience this is being loaded into by the client.
            _executionContext.LoadExecutionContextAsync(CartridgeExperienceKeyNames.SSMS_TsqlEditorChat, sqlService!).GetAwaiter().GetResult();

            var activeCartridge = cartridgeBootstrapper.LoadCartridge();
            activeCartridge.InitializeToolsetsAsync().GetAwaiter().GetResult();

            // Assign the kernel’s plugins and chat service
            kernel = builder.Build();
            var userChatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            Logger.Verbose("Kernel initialization completed");

            return kernel;
        }
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    public class ChatExecutionAccessChecker : IExecutionAccessChecker
    {
        private static string _listOfProcs = string.Empty;
        private string _coreInstructionsBegin = @"
                                      You are the SQL Server security guardian for the current database and server.
                                      Work step-by-step and do not skip any requirements.
                                      Your response must be exactly one of the following values:
                                      '" + ScriptExecutionRequirement.READ_ONLY + "', " +
                                      "'" + ScriptExecutionRequirement.READ_WRITE + "', " +
                                      "'" + ScriptExecutionRequirement.UNKNOWN + "'" +
                                      @"Do not include any other comments or explain your reasoning in the response.  And do not abbreviate or change the values in any way.
                                      You must assess the provided query and determine the type of access required to execute it.
                                      In your assessment, use your knowledge of SQL Server to determine what type of access.
                                      In addition to your knowledge see the {{SPECIAL INSTRUCTIONS} section for additional guidance.
                                      
                                      {{ACCESS MODE DEFINITIONS}}" +
                                      ScriptExecutionRequirement.READ_ONLY + ": the script requires read-only access and will not modify the database in any way.  the script does not include calls to any stored procedures that could alter the database or server." +
                                      ScriptExecutionRequirement.READ_WRITE + ": the script may alter the state of the database or server.  This includes any changes to metadata and userdata." +
                                      ScriptExecutionRequirement.UNKNOWN + ": the access mode cannot be clearly determined to need write access or not." +
                                      @"
                                      {{SPECIAL INSTRUCTIONS}}
                                      If the script creates temp tables, it should still be considered read-only as long as any inserts or modifications are constrained to the temp tables the script creted itself.
                                      Execution of any Stored Procedure (user or system) should be considered 'Write Access' unless it is in the list of {{ALLOWED_STORED_PROCEDURES}} which only require 'Read Access'

                                      {{ALLOWED_STORED_PROCEDURES}}";

        private string _coreInstructionsEnd =
                                    @"{{EXAMPLES}}
                                      INPUT: SELECT * FROM sys.objects
                                      RESPONSE: " + ScriptExecutionRequirement.READ_ONLY + @"
                                      INPUT: SELECT * FROM dbo.Sales
                                      RESPONSE: " + ScriptExecutionRequirement.READ_ONLY + @"
                                      INPUT: CREATE TABLE dbo.Sales (ID INT)
                                      RESPONSE: " + ScriptExecutionRequirement.READ_WRITE + @"
                                      INPUT: ALTER DATABASE [YourDatabaseName] SET AUTOMATIC_TUNING (FORCE_LAST_GOOD_PLAN = ON);
                                      RESPONSE: " + ScriptExecutionRequirement.READ_WRITE + @"
                                      ";

        private string _corePrompt = @"Work step-by-step and carefully determine the required role for this script: ";

        private string _queryToCheck = string.Empty;

        private IScriptoriaTrace _copilotLogger;


        /// <summary>
        /// utility class that is used to evaluate a given script and determine 
        /// the required access it needs.  It evaluates the script and will return
        /// a role (access mode) from CopilotAccessModes.
        /// 
        /// This is not an actual permissions check, true permissions are still the 
        /// sql permissions in the engine.  This layer is a behavioral role that the user
        /// can configure to have the copilot either return scripts to be copied,
        /// ensure that user approval is part of the flow, or go into a full 'developer' 
        /// mode where the copilot can execute any script the current login allows.
        /// </summary>
        /// <param name="vanillaKernel"></param>
        /// <param name="readOnlyProcs"></param>
        public ChatExecutionAccessChecker(IList<string> readOnlyProcs, IScriptoriaTrace copilotLogger)
        {
            ExecutionMode = CopilotAccessModes.READ_WRITE_NEVER;
            ActiveExchangeId = string.Empty;
            _listOfProcs = string.Join(",", readOnlyProcs);
            _copilotLogger = copilotLogger;
            _copilotLogger.WriteInfoEvent(ScriptoriaTraceEvents.ScriptoriaSessionStart, $"AccessChecker: list of read only procs:{_listOfProcs}");
        }

        /// <summary>
        /// what execution mode the copilot is currently running in.
        /// This will be a value from CopilotAccessModes
        /// </summary>
        public string ExecutionMode { get; set; }

        /// <summary>
        /// the client provided ID for the current prompt/response exchange being processed
        /// </summary>
        public string ActiveExchangeId { get; set; }

        /// <summary>
        /// Evaluate a given script to determine what type of access the script requires
        /// </summary>
        /// <param name="queryToCheck"></param>
        /// <returns>the access level required to execute the provided script</returns> 
        public async Task<string> CheckRequiredRoleAsync(string queryToCheck)
        {
            var shortQueryText = queryToCheck.Length > 30 ? $"{queryToCheck.Substring(0, 30)}..." : queryToCheck;
            _copilotLogger.WriteInfoEvent(ScriptoriaTraceEvents.KernelFunctionCall,
                $"Access check on query (first 30 chars shown): {shortQueryText}");

            // Generate a new conversation URI
            var conversationUri = Guid.NewGuid().ToString();

            // Create a new conversation object
            var conversation = new CopilotConversation
            {
                ConversationUri = conversationUri,
                CurrentMessage = "",
                SqlConnection = null,  // Not needed for validation
                State = new ConversationState(),
                CompletionSource = new TaskCompletionSource<ConversationState>()
            };

            // Register it in the conversation manager
            CopilotService.Instance.ConversationManager.AddOrUpdateConversation(conversationUri, conversation);

            // Construct the full system prompt
            var fullPrompt = _coreInstructionsBegin + "\n\n" + _corePrompt + queryToCheck + "\n\n" + _coreInstructionsEnd;

            // Prepare the LLM message
            var messages = new List<LanguageModelRequestMessage>
            {
                new LanguageModelRequestMessage
                {
                    Role = MessageRole.System,
                    Text = fullPrompt
                },
                new LanguageModelRequestMessage
                {
                    Role = MessageRole.User,
                    Text = "Please process this query per the system prompt: " + queryToCheck + "\n\n"
                },
            };

            // No tools needed for this validation request
            var tools = new List<LanguageModelChatTool>();

            // Queue the request asynchronously
            await CopilotService.Instance.ConversationManager.QueueLLMRequest(conversationUri, RequestMessageType.DirectRequest, messages, tools);

            // Wait for the response asynchronously
            var responseState = await conversation.CompletionSource.Task;

            // Extract and return the required role from the response
            var responseText = responseState.Response?.Trim() ?? ScriptExecutionRequirement.UNKNOWN;

            _copilotLogger.WriteInfoEvent(ScriptoriaTraceEvents.KernelFunctionCall,
                $"LLM Response received: {responseText}");

            // Map the response to expected enum values
            return responseText switch
            {
                ScriptExecutionRequirement.READ_ONLY => ScriptExecutionRequirement.READ_ONLY,
                ScriptExecutionRequirement.READ_WRITE => ScriptExecutionRequirement.READ_WRITE,
                _ => ScriptExecutionRequirement.UNKNOWN
            };
        }

        public string CheckRequiredRole(string queryToCheck)
        {
            return Task.Run(async () => await CheckRequiredRoleAsync(queryToCheck)).Result;
        }

        /// <summary>
        /// Instructions for the system prompt based on the current execution mode
        /// </summary>
        public string RoleInstructions
        {
            get
            {
                switch (ExecutionMode)
                {
                    // both write modes have the same instructions.  the approval is handled by the client API without the LLM knowing the difference.
                    case CopilotAccessModes.READ_WRITE_APPROVAL:
                    case CopilotAccessModes.READ_WRITE:
                        return @"
                            YOUR QUERY EXECUTION MODE: 
                                You are running in read-write mode.
                                You may directly execute the scripts necessary to answer the user's question or command.
                                ";

                    // default is read-only
                    case CopilotAccessModes.READ_WRITE_NEVER:
                    default:
                        return @"
                            YOUR QUERY EXECUTION MODE: 
                                You are running in a read-only mode.
                                You may execute queries that read system metadata or user data directly against the current database and server.
                                Any script that can in any way modify the state of the database or server, requires write access that you do not have.
                                For scripts that require write access you should validate the syntax of the query using ValidateGeneratedTSQL and then simply provide the script back to the user.
                                ";
                }
            }
        }
    }
}
