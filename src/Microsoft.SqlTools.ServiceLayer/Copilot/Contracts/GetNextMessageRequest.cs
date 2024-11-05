//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Copilot.Contracts
{
    public enum MessageType
    {
        MessageFragment = 0,

        MessageComplete = 1,

        RequestLLM = 2
    }

    public enum MessageRole
    {
        System = 0,

        User = 1,

        Assistant = 2,

        Tool = 3,

        Function = 4,
    }

    public class LanguageModelRequestMessage
    {
        public string Text { get; set; }

        public MessageRole Role { get; set; }
    }

    public class GetNextMessageParams
    {
        public string ConversationUri { get; set; }

        public string UserText { get; set; }

        public LanguageModelChatTool Tool { get; set; }

        public string ToolParameters { get; set; }
    }

    public class LanguageModelChatTool
    {
        public string FunctionName { get; set; }

        public string FunctionDescription { get; set; }

        public string FunctionParameters { get; set; }
    }

    public class GetNextMessageResponse
    {
        public MessageType MessageType { get; set; }

        public string ResponseText { get; set; }

        public LanguageModelChatTool[] Tools { get; set; }

        public LanguageModelRequestMessage[] RequestMessages { get; set; }
    }
   
    public class GetNextMessageRequest
    {
        public static readonly
            RequestType<GetNextMessageParams, GetNextMessageResponse> Type =
            RequestType<GetNextMessageParams, GetNextMessageResponse>.Create("copilot/getnextmessage");
    }
}
