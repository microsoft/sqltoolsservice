//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Chat;

namespace Microsoft.SqlTools.Connectors.VSCode;

/// <summary>
/// VSCodeChatMessageContentPart
/// </summary>
public class VSCodeChatMessageContentPart
{
    /// <summary>
    /// The text content.
    /// </summary>
    public string? Text { get; set; }
}


/// <summary>
/// OpenAI specialized chat message content
/// </summary>
public sealed class VSCodeChatMessageContent : Microsoft.SemanticKernel.ChatMessageContent
{
    /// <summary>
    /// Gets the metadata key for the tool id.
    /// </summary>
    public static string ToolIdProperty => "ChatCompletionsToolCall.Id";

    /// <summary>
    /// Gets the metadata key for the list of <see cref="ChatToolCall"/>.
    /// </summary>
    internal static string FunctionToolCallsProperty => "ChatResponseMessage.FunctionToolCalls";

    /// <summary>
    /// Initializes a new instance of the <see cref="VSCodeChatMessageContent"/> class.
    /// </summary>
    internal VSCodeChatMessageContent(LanguageModelChatCompletion completion, string modelId, IReadOnlyDictionary<string, object?>? metadata = null)
        : base(new AuthorRole(completion.Role.ToString()), CreateContentItems(completion.Content), modelId, completion, System.Text.Encoding.UTF8, CreateMetadataDictionary(completion.ToolCalls, metadata))
    {
        this.ToolCalls = completion.ToolCalls;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VSCodeChatMessageContent"/> class.
    /// </summary>
    internal VSCodeChatMessageContent(ChatMessageRole role, string? content, string modelId, IReadOnlyList<ChatToolCall> toolCalls, IReadOnlyDictionary<string, object?>? metadata = null)
        : base(new AuthorRole(role.ToString()), content, modelId, content, System.Text.Encoding.UTF8, CreateMetadataDictionary(toolCalls, metadata))
    {
        this.ToolCalls = toolCalls;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VSCodeChatMessageContent"/> class.
    /// </summary>
    internal VSCodeChatMessageContent(AuthorRole role, string? content, string modelId, IReadOnlyList<ChatToolCall> toolCalls, IReadOnlyDictionary<string, object?>? metadata = null)
        : base(role, content, modelId, content, System.Text.Encoding.UTF8, CreateMetadataDictionary(toolCalls, metadata))
    {
        this.ToolCalls = toolCalls;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VSCodeChatMessageContent"/> class.
    /// </summary>
    internal VSCodeChatMessageContent(ChatMessageRole role, string? content, string modelId, ChatTool responseTool, string responseToolParameters, IReadOnlyDictionary<string, object?>? metadata = null)
        : base(new AuthorRole(role.ToString()), content, modelId, content, System.Text.Encoding.UTF8, CreateMetadataDictionary(responseTool, metadata))
    {
        this.ResponseTool = responseTool;
        this.ResponseToolParameters = responseToolParameters;
    }

    private static ChatMessageContentItemCollection CreateContentItems(IReadOnlyList<VSCodeChatMessageContentPart> contentUpdate)
    {
        ChatMessageContentItemCollection collection = [];

        foreach (var part in contentUpdate)
        {
            // We only support text content for now.
            //if (part.Kind == ChatMessageContentPartKind.Text)
            //{
            collection.Add(new TextContent(part.Text));
            //}
        }

        return collection;
    }

    /// <summary>
    /// A list of the tools called by the model.
    /// </summary>
    public IReadOnlyList<ChatToolCall> ToolCalls { get; }

    /// <summary>
    /// ResponseTool
    /// </summary>
    public ChatTool ResponseTool { get;  }

    /// <summary>
    /// ResponseToolParameters
    /// </summary>
    public string ResponseToolParameters { get; }

    /// <summary>
    /// Retrieve the resulting function from the chat result.
    /// </summary>
    /// <returns>The <see cref="VSCodeFunctionToolCall"/>, or null if no function was returned by the model.</returns>
    public IReadOnlyList<VSCodeFunctionToolCall> GetOpenAIFunctionToolCalls()
    {
        List<VSCodeFunctionToolCall>? functionToolCallList = null;

        foreach (var toolCall in this.ToolCalls)
        {
            if (toolCall.Kind == ChatToolCallKind.Function)
            {
                (functionToolCallList ??= []).Add(new VSCodeFunctionToolCall(toolCall));
            }
        }

        if (functionToolCallList is not null)
        {
            return functionToolCallList;
        }

        return [];
    }

    private static IReadOnlyDictionary<string, object?>? CreateMetadataDictionary(
        IReadOnlyList<ChatToolCall> toolCalls,
        IReadOnlyDictionary<string, object?>? original)
    {
        // We only need to augment the metadata if there are any tool calls.
        if (toolCalls.Count > 0)
        {
            Dictionary<string, object?> newDictionary;
            if (original is null)
            {
                // There's no existing metadata to clone; just allocate a new dictionary.
                newDictionary = new Dictionary<string, object?>(1);
            }
            else if (original is IDictionary<string, object?> origIDictionary)
            {
                // Efficiently clone the old dictionary to a new one.
                newDictionary = new Dictionary<string, object?>(origIDictionary);
            }
            else
            {
                // There's metadata to clone but we have to do so one item at a time.
                newDictionary = new Dictionary<string, object?>(original.Count + 1);
                foreach (var kvp in original)
                {
                    newDictionary[kvp.Key] = kvp.Value;
                }
            }

            // Add the additional entry.
            newDictionary.Add(FunctionToolCallsProperty, toolCalls.Where(ctc => ctc.Kind == ChatToolCallKind.Function).ToList());

            return newDictionary;
        }

        return original;
    }

    private static IReadOnlyDictionary<string, object?>? CreateMetadataDictionary(
        ChatTool responseTool,
        IReadOnlyDictionary<string, object?>? original)
    {
        // We only need to augment the metadata if there are any tool calls.
        //if (toolCalls.Count > 0)
        //{
        Dictionary<string, object?> newDictionary;
        if (original is null)
        {
            // There's no existing metadata to clone; just allocate a new dictionary.
            newDictionary = new Dictionary<string, object?>(1);
        }
        else if (original is IDictionary<string, object?> origIDictionary)
        {
            // Efficiently clone the old dictionary to a new one.
            newDictionary = new Dictionary<string, object?>(origIDictionary);
        }
        else
        {
            // There's metadata to clone but we have to do so one item at a time.
            newDictionary = new Dictionary<string, object?>(original.Count + 1);
            foreach (var kvp in original)
            {
                newDictionary[kvp.Key] = kvp.Value;
            }
        }

        // Add the additional entry.
        newDictionary.Add(FunctionToolCallsProperty, responseTool // toolCalls.Where(ctc => ctc.Kind == ChatToolCallKind.Function).ToList()
            );

        return newDictionary;
        // }

        //return original;
    }
}
