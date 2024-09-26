//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ClientModel;
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
using static Microsoft.SqlTools.ServiceLayer.Copilot.CopilotConversationManager;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    /// <summary>
    /// VSCodeChatMessageContentPart
    /// </summary>
    // public class VSCodeChatMessageContentPart
    // {
    //     /// <summary>
    //     /// The text content.
    //     /// </summary>
    //     public string? Text { get; set; }
    // }

    /// <summary>
    /// Implementation of collection abstraction over streaming chat updates.
    /// </summary>
#pragma warning disable CA1711
    public class VSCodeAsyncChatCompletionCollection : AsyncCollectionResult<LanguageModelChatCompletion>
    {
        private IList<LanguageModelRequestMessage> messages;
        private IList<LanguageModelChatTool> tools;

        private IList<ChatTool> copilotTools;

        private AutoResetEvent responseReadyEvent = new AutoResetEvent(false);
        private LLMRequest request;

        public LLMRequest Request { get { return request; } }

        public IList<ChatTool> CopilotTools { get { return copilotTools; } }

        public VSCodeAsyncChatCompletionCollection(
            ChatHistory chat,
            IList<ChatTool> tools
        ) : base()
        {
            Debug.Assert(chat is not null);
            Debug.Assert(tools is not null);

            this.messages = VSCodeAsyncChatCompletionCollection.FromChatMessages(chat);
            this.tools = VSCodeAsyncChatCompletionCollection.FromChatTools(tools);
            this.copilotTools = tools;

            // bool hasToolResponse = chat.Any(message => message.Role == AuthorRole.Tool);

            request = CopilotService.Instance.ConversationManager.RequestLLM(
                "uri", this.messages, 
                this.tools, 
                this.responseReadyEvent);
                //hasToolResponse ? [] :this.tools, this.responseReadyEvent);
         }

        private static  IList<LanguageModelChatTool> FromChatTools(IList<ChatTool> tools)
        {
            var result = new List<LanguageModelChatTool>();
            foreach (var tool in tools)
            {
                result.Add(new LanguageModelChatTool()
                {
                    FunctionName = tool.FunctionName,
                    FunctionDescription = tool.FunctionDescription,
                    FunctionParameters = tool.FunctionParameters.SafeToString(),
                });
            }
            return result;
        }

        private static  IList<LanguageModelRequestMessage> FromChatMessages(ChatHistory chatHistory)
        {
            var result = new List<LanguageModelRequestMessage>();
            foreach (var message in chatHistory)
            {
                if (message.Role == AuthorRole.System || message.Role == AuthorRole.Tool)
                {
                    result.Add(new LanguageModelRequestMessage()
                    {
                        Text = message.Content,
                        Role = MessageRole.System
                    });
                }
                else if (message.Role == AuthorRole.User)
                {
                    if (message.Items is { Count: 1 } && message.Items.FirstOrDefault() is TextContent textContent)
                    {
                        result.Add(new LanguageModelRequestMessage()
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

            if (result.Last().Role == MessageRole.System)
            {
                result.Add(new LanguageModelRequestMessage()
                {
                    Text = "Above messages contain the results of calling a function. The user cannot see this result, so you should explain it to the user if referencing it in your answer.;",
                    Role = MessageRole.User
                });
            }

            return result;
        }

        /// <summary>
        /// GetAsyncEnumerator
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override IAsyncEnumerator<LanguageModelChatCompletion> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
#pragma warning disable CA2000
            return new AsyncStreamingChatUpdateEnumerator(this, cancellationToken);
#pragma warning restore CA2000
        }

        private sealed class AsyncStreamingChatUpdateEnumerator : IAsyncEnumerator<LanguageModelChatCompletion>
        {
            // private static ReadOnlySpan<byte> TerminalData => "[DONE]"u8;
            // private readonly List<LanguageModelChatCompletion> _completions;
            // private int _position = 0;

            private LanguageModelChatCompletion? _current;
            private LLMRequest request;
            private bool processed = false;
            private VSCodeAsyncChatCompletionCollection enumerable;

            /// <summary>
            /// AsyncStreamingChatUpdateEnumerator
            /// </summary>
            /// <param name="enumerable"></param>
            /// <param name="cancellationToken"></param>
            public AsyncStreamingChatUpdateEnumerator(/*Func<Task<ClientResult>> getResultAsync,*/
                VSCodeAsyncChatCompletionCollection enumerable,
                CancellationToken cancellationToken)
            {
                // Debug.Assert(getResultAsync is not null);
                Debug.Assert(enumerable is not null);

                this.enumerable = enumerable;
                this.request = enumerable.Request;
            }

            LanguageModelChatCompletion IAsyncEnumerator<LanguageModelChatCompletion>.Current
                => this._current!;

            async ValueTask<bool> IAsyncEnumerator<LanguageModelChatCompletion>.MoveNextAsync()
            {
                if (processed)
                {
                    return false;
                }

                this.request.RequestCompleteEvent.WaitOne();

                await Task.FromResult(Task.CompletedTask).ConfigureAwait(false);

                var completion = new LanguageModelChatCompletion();
                completion.Content = new List<VSCodeChatMessageContentPart>();
                var contentPart = new VSCodeChatMessageContentPart()
                {
                    Text = this.request.Response
                };
                completion.Content.Add(contentPart);

                if (this.request.ResponseTool != null)
                {
                    completion.ResponseTool = this.enumerable.CopilotTools.First(tool => tool.FunctionName == this.request.ResponseTool.FunctionName);
                    completion.ResponseToolParameters = this.request.ResponseToolParameters;
                }

                this._current = completion;
                processed = true;
                return true;
            }

            public async ValueTask DisposeAsync()
            {
                //await DisposeAsyncCore().ConfigureAwait(false);
                await Task.FromResult(Task.CompletedTask).ConfigureAwait(false);
                GC.SuppressFinalize(this);
            }
        }
    }
}
