//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
    public class VSCodeAsyncChatCompletionCollection : VSCodeAsyncCollectionResult<LanguageModelChatCompletion>
    {
        private readonly IList<LanguageModelRequestMessage> _messages;
        private readonly IList<LanguageModelChatTool> _tools;
        private readonly IList<ChatTool> _copilotTools;
        private readonly LLMRequest _request;

        public VSCodeAsyncChatCompletionCollection(
            ChatHistory chat,
            IList<ChatTool> tools)
        {
            Debug.Assert(chat is not null);
            Debug.Assert(tools is not null);

            _copilotTools = tools;
            _messages = FromChatMessages(chat);
            _tools = FromChatTools(tools);

            // Create the request with an AutoResetEvent
            _request = CopilotService.Instance.ConversationManager.RequestLLM(
                "uri", _messages, _tools, new AutoResetEvent(false));
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
            return new AsyncStreamingChatUpdateEnumerator(_request, _copilotTools);
        }

        private sealed class AsyncStreamingChatUpdateEnumerator : IAsyncEnumerator<LanguageModelChatCompletion>
        {
            private readonly LLMRequest _request;
            private readonly IList<ChatTool> _tools;
            private LanguageModelChatCompletion? _current;
            private bool _processed;

            public AsyncStreamingChatUpdateEnumerator(
                LLMRequest request,
                IList<ChatTool> tools)
            {
                _request = request;
                _tools = tools;
            }

            public LanguageModelChatCompletion Current => _current!;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_processed)
                    return false;

                await Task.Run(_request.RequestCompleteEvent.WaitOne).ConfigureAwait(false);

                _current = CreateCompletion(
                    _request.Response,
                    _request.ResponseTool,
                    _request.ResponseToolParameters);

                _processed = true;
                return true;
            }

            private LanguageModelChatCompletion CreateCompletion(
                string response,
                LanguageModelChatTool? responseTool,
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
                    completion.ResponseTool = _tools.First(t =>
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
