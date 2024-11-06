//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SqlTools.Connectors.VSCode;
using OpenAI.Chat;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    internal class VSCodeLanguageModelEndpoint : ILanguageModelEndpoint
    {
        public LanguageModelChatCompletion SendChatRequest(ChatHistory chatHistory, IList<ChatTool> tools)
        {
            return new LanguageModelChatCompletion
            {
                ResultText = "Non-async message - result text",
            };
        }

        public VSCodeAsyncCollectionResult<LanguageModelChatCompletion> SendChatRequestStreamingAsync(
            ChatHistory chatHistory, 
            IList<ChatTool> tools)
        {
            return new VSCodeAsyncChatCompletionCollection(chatHistory, tools);
        }
    }
}