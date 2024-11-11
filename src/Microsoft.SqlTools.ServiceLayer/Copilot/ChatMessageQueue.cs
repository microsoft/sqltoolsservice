//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
    
#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.SqlServer.SqlCopilot.Common;
using Microsoft.SqlTools.ServiceLayer.Copilot.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    public class LLMRequest
    {
        public string ConversationUri { get; set; }
        public IList<LanguageModelRequestMessage> Messages { get; set; }
        public IList<LanguageModelChatTool> Tools { get; set; }
        public string Response { get; set; }
        public LanguageModelChatTool ResponseTool { get; set; }
        public string ResponseToolParameters { get; set; }
        public AutoResetEvent RequestCompleteEvent { get; set; }
    }

    public enum RequestMessageType
    {
        StartConversation,
        ToolCallRequest,
        Response
    }

    public record ChatMessage(
        RequestMessageType Type,
        string ConversationUri,
        IList<LanguageModelRequestMessage> Messages,
        IList<LanguageModelChatTool> Tools,
        AutoResetEvent ResponseReadyEvent,
        CopilotConversation Conversation
    );

    public class ChatMessageQueue
    {
        private readonly AutoResetEvent requestEvent = new(false);
        private readonly Queue<LLMRequest> requestQueue = new();
        //private LLMRequest pendingRequest;
        private readonly ConcurrentDictionary<string, CopilotConversation> conversations;
        private readonly ConcurrentDictionary<string, HashSet<string>> toolCallCache = new();
        private string CreateToolCallKey(string toolName, string parameters) => 
            $"{toolName}:{parameters}";


        private readonly Channel<ChatMessage> messageChannel = Channel.CreateUnbounded<ChatMessage>();


        public ChatMessageQueue(ConcurrentDictionary<string, CopilotConversation> conversations)
        {
            this.conversations = conversations;
        }

        // Expose this for WaitAny
        public AutoResetEvent RequestEvent => requestEvent;


        public async Task EnqueueMessageAsync(ChatMessage message)
        {
            await messageChannel.Writer.WriteAsync(message);

            var request = new LLMRequest
            {
                ConversationUri = message.ConversationUri,
                Messages = message.Messages,
                Tools = message.Tools,
                RequestCompleteEvent = message.ResponseReadyEvent
            };

            requestQueue.Enqueue(request);
            requestEvent.Set();
        }

        public GetNextMessageResponse ProcessNextMessage(
            string conversationUri,
            string userText,
            LanguageModelChatTool tool,
            string toolParameters)
        {
            
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

            ConversationState state = conversation.State;
        
            // Handle tool response or user text
            if (tool != null && state != null)
            {
                NotifyToolCallRequest(tool, toolParameters, state);
            }
            else if (!string.IsNullOrEmpty(userText) && state != null)
            {
                NotifyUserTextRequest(userText, state);
            }

            // Wait on all events
            int eventIdx = WaitForMessage();

            // Handle new LLM request
            if (IsLanguageModelRequestMessage(eventIdx))
            {
                var nextRequest = requestQueue.Dequeue();
                conversation.State = new ConversationState
                {
                    RequestCompleteEvent = nextRequest.RequestCompleteEvent,
                    Response = nextRequest.Messages.Last().Text,
                };
                return new GetNextMessageResponse
                {
                    MessageType = MessageType.RequestLLM,
                    ResponseText = nextRequest.Messages.Last().Text,
                    Tools = nextRequest.Tools.ToArray(),
                    RequestMessages = nextRequest.Messages.ToArray()
                };        
            }

            

            return new GetNextMessageResponse
            {
                MessageType = MessageType.MessageComplete,
                ResponseText = responseText
            };
        }

        // public async Task StartProcessingAsync()
        // {
        //     await foreach (var message in messageChannel.Reader.ReadAllAsync())
        //     {
        //         await ProcessMessageAsync(message);  // Process each message as it arrives
        //     }
        // }

        // private async Task ProcessMessageAsync(ChatMessage message)
        // {
        //     switch (message.Type)
        //     {
        //         // case MessageType.StartConversation:
        //         //     await HandleStartConversationAsync(message);
        //         //     break;
                
        //         case RequestMessageType.ToolCallRequest:
        //             await HandleToolCallRequestAsync(message);
        //             break;

        //         // case MessageType.Response:
        //         //     SetResponse(message.ConversationId, message.Response);
        //         //     break;
        //     }
        // }

        public LLMRequest EnqueueRequest(
            string conversationUri,
            IList<LanguageModelRequestMessage> messages,
            IList<LanguageModelChatTool> tools,
            AutoResetEvent responseReadyEvent)
        {
            var request = new LLMRequest
            {
                ConversationUri = conversationUri,
                Messages = messages,
                Tools = tools,
                RequestCompleteEvent = responseReadyEvent
            };

            requestQueue.Enqueue(request);
            requestEvent.Set();
            return request;
        }

        private bool HandleToolCallMessage(string conversationUri, LanguageModelChatTool tool, string toolParameters, out GetNextMessageResponse response)
        {
            response = null;
            var callKey = CreateToolCallKey(tool.FunctionName, toolParameters);
            var conversationCache = toolCallCache.GetOrAdd(
                conversationUri, 
                _ => new HashSet<string>());

            if (!conversationCache.Add(callKey))
            {
                SqlCopilotTrace.WriteInfoEvent(
                    SqlCopilotTraceEvents.KernelFunctionCall,
                    $"Skipping repeated tool call: {callKey}");
                
                response = new GetNextMessageResponse
                {
                    MessageType = MessageType.MessageComplete,
                    ResponseText = string.Empty
                };
                
                return true; // Indicate that processing should stop
            }

            return false; // Indicate that processing should continue
        }

        private void NotifyToolCallRequest(LanguageModelChatTool tool, string toolParameters, ConversationState state)
        {
            state.ResponseTool = tool;
            state.ResponseToolParameters = toolParameters;
            state.RequestCompleteEvent.Set();
        }

        private void NotifyUserTextRequest(string userText, ConversationState state)
        {
            state.Response = userText;
            state.RequestCompleteEvent.Set();
        }

        private int WaitForMessage()
        {
            var events = new AutoResetEvent[conversations.Count + 1];
            events[conversations.Count] = requestEvent;
            int i = 0;
            foreach (var c in conversations)
            {
                events[i++] = c.Value.MessageCompleteEvent;
            }

            return WaitHandle.WaitAny(events);
        }

        private bool IsLanguageModelRequestMessage(int eventIdx) =>
            eventIdx == conversations.Count && requestQueue.Count > 0;
    }
}