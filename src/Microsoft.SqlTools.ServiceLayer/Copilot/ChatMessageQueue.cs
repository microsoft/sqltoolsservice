//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
    
#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Copilot.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    public enum RequestMessageType
    {
        ToolCallRequest,
        DirectRequest,
        Response
    }

    public record ChatMessage(
        RequestMessageType Type,
        string ConversationUri,
        IList<LanguageModelRequestMessage> Messages,
        IList<LanguageModelChatTool> Tools,
        CopilotConversation Conversation
    );

    public class ChatMessageQueue
    {
        private readonly ConcurrentDictionary<string, CopilotConversation> conversations;
        private readonly ConcurrentDictionary<string, HashSet<string>> toolCallCache = new();
        private string CreateToolCallKey(string toolName, string parameters) => 
            $"{toolName}:{parameters}";

        private readonly Channel<ChatMessage> messageChannel = Channel.CreateUnbounded<ChatMessage>();


        public ChatMessageQueue(ConcurrentDictionary<string, CopilotConversation> conversations)
        {
            this.conversations = conversations;
        }

        public async Task EnqueueMessageAsync(ChatMessage message)
        {
            await messageChannel.Writer.WriteAsync(message);
        }

        public async Task<GetNextMessageResponse> ProcessNextMessage(
            string conversationUri,
            string userText,
            LanguageModelChatTool tool,
            string toolParameters)
        {

            Logger.Verbose($"ProcessNextMessage: Conversation '{conversationUri}' for text '{userText}'");

            if (tool != null && HandleToolCallMessage(conversationUri, tool, toolParameters, out var response))
            {
                return response;
            }

            // Handle conversation message
            string responseText = string.Empty;
            if (conversations.TryGetValue(conversationUri, out var conversation))
            {
                responseText = conversation.CurrentMessage;
            }
        
            // Handle tool response or user text
            if (tool != null && conversation.State != null)
            {
                NotifyToolCallRequest(tool, toolParameters, conversation);
            }
            else if (!string.IsNullOrEmpty(userText) && conversation.State != null)
            {
                NotifyUserTextRequest(userText, conversation);
            }

            var message = await messageChannel.Reader.ReadAsync();
            if (message.Type == RequestMessageType.ToolCallRequest || message.Type == RequestMessageType.DirectRequest)
            {
                conversation.State = new ConversationState
                {
                    Response = message.Messages.Last().Text,
                };
                return new GetNextMessageResponse
                {
                    ConversationUri = message.ConversationUri,
                    MessageType = message.Type == RequestMessageType.DirectRequest ? MessageType.RequestDirectLLM : MessageType.RequestLLM,
                    ResponseText = message.Conversation.CurrentMessage,
                    Tools = message.Tools.ToArray(),
                    RequestMessages = message.Messages.ToArray()
                };
            }

            return new GetNextMessageResponse
            {
                ConversationUri = message.ConversationUri,
                MessageType = MessageType.MessageComplete,
                ResponseText = responseText
            };

        }

        private bool HandleToolCallMessage(string conversationUri, LanguageModelChatTool tool, string toolParameters, out GetNextMessageResponse response)
        {
            Logger.Verbose($"HandleToolCallMessage: Conversation '{conversationUri}' for tool '{tool.FunctionName}'");

            response = null;
            var callKey = CreateToolCallKey(tool.FunctionName, toolParameters);
            var conversationCache = toolCallCache.GetOrAdd(
                conversationUri, 
                _ => new HashSet<string>());

            if (!conversationCache.Add(callKey))
            {
                Logger.Verbose($"Skipping repeated tool call: {callKey}");
                
                response = new GetNextMessageResponse
                {
                    MessageType = MessageType.MessageComplete,
                    ResponseText = string.Empty
                };
                
                return true; // Indicate that processing should stop
            }

            return false; // Indicate that processing should continue
        }

        private void NotifyToolCallRequest(LanguageModelChatTool tool, string toolParameters, CopilotConversation conversation)
        {
            Logger.Verbose($"NotifyToolCallRequest: Conversation '{conversation.ConversationUri}' for tool '{tool.FunctionName}'");

            conversation.State.ResponseTool = tool;
            conversation.State.ResponseToolParameters = toolParameters;
            conversation.CompletionSource.TrySetResult(conversation.State);
        }

        private void NotifyUserTextRequest(string userText, CopilotConversation conversation)
        {
            Logger.Verbose($"NotifyToolCallRequest: Conversation '{conversation.ConversationUri}' for text '{userText}'");

            conversation.State.Response = userText;
            conversation.CompletionSource.TrySetResult(conversation.State);
        }
    }
}
