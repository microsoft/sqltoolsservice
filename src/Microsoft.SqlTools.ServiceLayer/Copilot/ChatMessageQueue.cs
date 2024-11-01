//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
    
#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    public class ChatMessageQueue
    {
        private readonly AutoResetEvent _requestEvent = new(false);
        private readonly Queue<LLMRequest> _requestQueue = new();
        private LLMRequest _pendingRequest;
        private readonly ConcurrentDictionary<string, CopilotConversation> _conversations;
        private readonly ConcurrentDictionary<string, HashSet<string>> _toolCallCache = new();
        private string CreateToolCallKey(string toolName, string parameters) => 
            $"{toolName}:{parameters}";

        public ChatMessageQueue(ConcurrentDictionary<string, CopilotConversation> conversations)
        {
            _conversations = conversations;
        }

        // Expose this for WaitAny
        public AutoResetEvent RequestEvent => _requestEvent;

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

            _requestQueue.Enqueue(request);
            _requestEvent.Set();
            return request;
        }

        public GetNextMessageResponse ProcessNextMessage(
            string conversationUri,
            string userText,
            LanguageModelChatTool tool,
            string toolParameters)
        {
            if (tool != null)
            {
                var callKey = CreateToolCallKey(tool.FunctionName, toolParameters);
                var conversationCache = _toolCallCache.GetOrAdd(
                    conversationUri, 
                    _ => new HashSet<string>());

                // If we've seen this exact call before, ignore it
                if (!conversationCache.Add(callKey))
                {
                    SqlCopilotTrace.WriteInfoEvent(
                        SqlCopilotTraceEvents.KernelFunctionCall,
                        $"Skipping repeated tool call: {callKey}");
                    return new GetNextMessageResponse
                    {
                        MessageType = MessageType.MessageComplete,
                        ResponseText = string.Empty
                    };
                }
            }

            // Handle tool response or user text
            if (tool != null && _pendingRequest != null)
            {
                _pendingRequest.ResponseTool = tool;
                _pendingRequest.ResponseToolParameters = toolParameters;
                _pendingRequest.RequestCompleteEvent.Set();
                _pendingRequest = null;
            }
            else if (!string.IsNullOrEmpty(userText) && _pendingRequest != null)
            {
                _pendingRequest.Response = userText;
                _pendingRequest.RequestCompleteEvent.Set();
                _pendingRequest = null;
            }

            // Wait on all events
            var events = new AutoResetEvent[_conversations.Count + 1];
            events[_conversations.Count] = _requestEvent;
            int i = 0;
            foreach (var c in _conversations)
            {
                events[i++] = c.Value.MessageCompleteEvent;
            }

            int eventIdx = WaitHandle.WaitAny(events);

            // Handle new LLM request
            if (eventIdx == _conversations.Count && _requestQueue.Count > 0)
            {
                _pendingRequest = _requestQueue.Dequeue();
                return new GetNextMessageResponse
                {
                    MessageType = MessageType.RequestLLM,
                    ResponseText = _pendingRequest.Messages.Last().Text,
                    Tools = _pendingRequest.Tools.ToArray(),
                    RequestMessages = _pendingRequest.Messages.ToArray()
                };
            }

            // Handle conversation message
            if (_conversations.TryGetValue(conversationUri, out var conversation))
            {
                return new GetNextMessageResponse
                {
                    MessageType = MessageType.MessageComplete,
                    ResponseText = conversation.CurrentMessage
                };
            }

            return new GetNextMessageResponse
            {
                MessageType = MessageType.MessageComplete,
                ResponseText = string.Empty
            };
        }
    }
}