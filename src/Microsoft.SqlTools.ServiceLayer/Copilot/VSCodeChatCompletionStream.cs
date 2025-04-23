//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SqlTools.Connectors.VSCode;
using Microsoft.SqlTools.ServiceLayer.Copilot.Contracts;
using Microsoft.SqlTools.Utility;
using OpenAI.Chat;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    public class VSCodeChatCompletionStream : IVSCodeStreamResult<LanguageModelChatCompletion>
    {
        private readonly IList<LanguageModelRequestMessage> messages;
        private readonly IList<LanguageModelChatTool> tools;
        private readonly IList<ChatTool> copilotTools;
        private readonly CopilotConversation conversation;
        private ChatMessage request;
        private RequestMessageType requestMessageType;

        private VSCodeChatCompletionStream(
            CopilotConversation conversation,
            ChatHistory chat,
            IList<ChatTool> tools,
            RequestMessageType requestMessageType)
        {
            Debug.Assert(chat is not null);
            Debug.Assert(tools is not null);

            this.copilotTools = tools;
            this.conversation = conversation;
            this.messages = FromChatMessages(chat);
            this.tools = FromChatTools(tools);
            this.requestMessageType = requestMessageType;

        }

        private async Task InitializeAsync()
        {
            // Queue the request asynchronously via the channel in ChatMessageQueue
            request = await CopilotService.Instance.ConversationManager.QueueLLMRequest(
                this.conversation.ConversationUri, this.requestMessageType, messages, tools);
        }

        public static async Task<VSCodeChatCompletionStream> CreateAsync(
            CopilotConversation conversation,
            ChatHistory chat,
            IList<ChatTool> tools,
            RequestMessageType requestMessageType)
        {
            var collection = new VSCodeChatCompletionStream(conversation, chat, tools, requestMessageType);
            await collection.InitializeAsync();
            return collection;
        }

        private static IList<LanguageModelChatTool> FromChatTools(IList<ChatTool> tools)
        {
            return tools.Select(tool => new LanguageModelChatTool
            {
                FunctionName = tool.FunctionName,
                FunctionDescription = tool.FunctionDescription,
                FunctionParameters = tool.FunctionParameters.SafeToString()
            }).ToList();
        }

        private static IList<LanguageModelRequestMessage> FromChatMessages(ChatHistory chatHistory)
        {
            var result = new List<LanguageModelRequestMessage>();

            foreach (var message in chatHistory)
            {
                if (message.Role == AuthorRole.System || message.Role == AuthorRole.Tool)
                {
                    result.Add(new LanguageModelRequestMessage
                    {
                        Text = message.Content,
                        Role = MessageRole.System
                    });
                }
                else if (message.Role == AuthorRole.User)
                {
                    if (message.Items is { Count: 1 } &&
                        message.Items.FirstOrDefault() is TextContent textContent)
                    {
                        result.Add(new LanguageModelRequestMessage
                        {
                            Text = textContent.Text,
                            Role = MessageRole.User
                        });
                    }
                    else
                    {
                        throw new Exception("Unsupported chat message content type.");
                    }
                }
            }

            // Add explanation message if last message was system
            if (result.Count > 0 && result[^1].Role == MessageRole.System)
            {
                result.Add(new LanguageModelRequestMessage
                {
                    Text = "Above messages contain the results of calling a function. " +
                          "The user cannot see this result, so you should explain it to the user if referencing it in your answer.",
                    Role = MessageRole.User
                });
            }

            return result;
        }

        public IAsyncEnumerator<LanguageModelChatCompletion> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            return new AsyncStreamingChatUpdateEnumerator(request, copilotTools);
        }

        private sealed class AsyncStreamingChatUpdateEnumerator : IAsyncEnumerator<LanguageModelChatCompletion>
        {
            private readonly ChatMessage request;
            private readonly IList<ChatTool> tools;
            private LanguageModelChatCompletion _current;
            private bool _processed;

            public AsyncStreamingChatUpdateEnumerator(
                ChatMessage request,
                IList<ChatTool> tools)
            {
                this.request = request;
                this.tools = tools;
            }

            public LanguageModelChatCompletion Current => _current!;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_processed)
                    return false;

                ConversationState state = await request.Conversation.CompletionSource.Task.ConfigureAwait(false);
                request.Conversation.CompletionSource = new TaskCompletionSource<ConversationState>();

                _current = CreateCompletion(
                    state.Response,
                    state.ResponseTool,
                    state.ResponseToolParameters);

                _processed = true;
                return true;
            }

            private LanguageModelChatCompletion CreateCompletion(
                string response,
                LanguageModelChatTool responseTool,
                string? toolParameters)
            {
                var completion = new LanguageModelChatCompletion
                {
                    Content = new List<VSCodeChatMessageContentPart>
                    {
                        new() { Text = response }
                    }
                };

                if (responseTool != null)
                {
                    completion.ResponseTool = tools.First(t =>
                        t.FunctionName == responseTool.FunctionName);
                    completion.ResponseToolParameters = toolParameters;
                }

                return completion;
            }

            public ValueTask DisposeAsync()
            {
                GC.SuppressFinalize(this);
                return ValueTask.CompletedTask;
            }
        }
    }
}
