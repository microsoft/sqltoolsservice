//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Scriptoria.Interfaces;
using Microsoft.Scriptoria.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SqlTools.ServiceLayer.Copilot.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    /// <summary>
    /// VS Code-specific LLM invoker for minion calls (AccessChecker, KnowledgeLibrarian, etc.)
    /// Uses DirectRequest to prevent responses from being streamed to users
    /// </summary>
    public class VSCodeMinionLLMInvoker : ILLMInvoker
    {
        private readonly CopilotConversation _conversation;
        private readonly IScriptoriaTrace _scriptoriaTrace;

        public VSCodeMinionLLMInvoker(CopilotConversation conversation, IScriptoriaTrace scriptoriaTrace)
        {
            _conversation = conversation;
            _scriptoriaTrace = scriptoriaTrace;
        }

        /// <inheritdoc />
        public async Task<string> InvokeAsync(
            ChatHistory chatHistory,
            Microsoft.SemanticKernel.Kernel kernel,
            ICartridge? activeCartridge,
            AIServiceSettings? settings,
            CancellationToken cancellationToken)
        {
            _scriptoriaTrace.WriteInfoEvent(Microsoft.Scriptoria.Common.ScriptoriaTraceEvents.KernelFunctionCall,
                "VSCodeMinionLLMInvoker: Invoking minion LLM call with DirectRequest");

            // Generate a new conversation URI for this minion call
            var conversationUri = Guid.NewGuid().ToString();

            // Create a temporary conversation object for this minion call
            var minionConversation = new CopilotConversation
            {
                ConversationUri = conversationUri,
                CurrentMessage = "",
                SqlConnection = _conversation.SqlConnection,
                State = new ConversationState(),
                CompletionSource = new TaskCompletionSource<ConversationState>()
            };

            // Register it in the conversation manager
            CopilotService.Instance.ConversationManager.AddOrUpdateConversation(conversationUri, minionConversation);

            try
            {
                // Convert ChatHistory to LanguageModelRequestMessages
                var messages = new List<LanguageModelRequestMessage>();
                foreach (var message in chatHistory)
                {
                    MessageRole role;
                    if (message.Role == Microsoft.SemanticKernel.ChatCompletion.AuthorRole.System)
                    {
                        role = MessageRole.System;
                    }
                    else if (message.Role == Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Assistant)
                    {
                        role = MessageRole.Assistant;
                    }
                    else
                    {
                        role = MessageRole.User;
                    }

                    messages.Add(new LanguageModelRequestMessage
                    {
                        Role = role,
                        Text = message.Content
                    });
                }

                // No tools needed for minion calls
                var tools = new List<LanguageModelChatTool>();

                // Queue the request with DirectRequest to prevent streaming to users
                await CopilotService.Instance.ConversationManager.QueueLLMRequest(
                    conversationUri, 
                    RequestMessageType.DirectRequest, 
                    messages, 
                    tools);

                // Wait for the response
                var responseState = await minionConversation.CompletionSource.Task;

                // Extract and return the response text
                var responseText = responseState.Response?.Trim() ?? string.Empty;

                _scriptoriaTrace.WriteInfoEvent(Microsoft.Scriptoria.Common.ScriptoriaTraceEvents.KernelFunctionCall,
                    $"VSCodeMinionLLMInvoker: Response received (length: {responseText.Length})");

                return responseText;
            }
            finally
            {
                // Clean up the temporary conversation
                CopilotService.Instance.ConversationManager.RemoveConversation(conversationUri);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<string> InvokeStreamingAsync(
            ChatHistory chatHistory,
            Microsoft.SemanticKernel.Kernel kernel,
            ICartridge? activeCartridge,
            AIServiceSettings? settings,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Minions should not use streaming, but if called, fall back to non-streaming
            var result = await InvokeAsync(chatHistory, kernel, activeCartridge, settings, cancellationToken);
            yield return result;
        }
    }
}
