﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Chat;

namespace Microsoft.SqlTools.Connectors.VSCode;

/// <summary>
/// VS Code specialized streaming chat message content.
/// </summary>
/// <remarks>
/// Represents a chat message content chunk that was streamed from the remote model.
/// </remarks>
public sealed class VSCodeStreamingChatMessageContent : StreamingChatMessageContent
{
    /// <summary>
    /// The reason why the completion finished.
    /// </summary>
    public ChatFinishReason? FinishReason { get; set; }

    /// <summary>
    /// Create a new instance of the <see cref="VSCodeStreamingChatMessageContent"/> class.
    /// </summary>
    /// <param name="chatUpdate">Internal OpenAI SDK Message update representation</param>
    /// <param name="choiceIndex">Index of the choice</param>
    /// <param name="modelId">The model ID used to generate the content</param>
    /// <param name="metadata">Additional metadata</param>
    internal VSCodeStreamingChatMessageContent(
        LanguageModelChatCompletion chatUpdate,
        int choiceIndex,
        string modelId,
        IReadOnlyDictionary<string, object?>? metadata = null)
        : base(
            null,
            null,
            chatUpdate,
            choiceIndex,
            modelId,
            Encoding.UTF8,
            metadata)
    {
        try
        {
            this.FinishReason = chatUpdate.FinishReason;

            if (chatUpdate.Role.HasValue)
            {
                this.Role = new AuthorRole(chatUpdate.Role.ToString()!);
            }

            if (chatUpdate.ToolCallUpdates is not null)
            {
                this.ToolCallUpdates = chatUpdate.ToolCallUpdates;
            }

            if (chatUpdate.ContentUpdate is not null)
            {
                this.Items = CreateContentItems(chatUpdate.ContentUpdate);
            }
        }
        catch (NullReferenceException)
        {
            // Temporary bugfix for: https://github.com/openai/openai-dotnet/issues/198
            // TODO: Remove this try-catch block once the bug is fixed.
        }
    }

    /// <summary>
    /// Create a new instance of the <see cref="VSCodeStreamingChatMessageContent"/> class.
    /// </summary>
    /// <param name="authorRole">Author role of the message</param>
    /// <param name="content">Content of the message</param>
    /// <param name="toolCallUpdates">Tool call updates</param>
    /// <param name="completionsFinishReason">Completion finish reason</param>
    /// <param name="choiceIndex">Index of the choice</param>
    /// <param name="modelId">The model ID used to generate the content</param>
    /// <param name="metadata">Additional metadata</param>
    internal VSCodeStreamingChatMessageContent(
        AuthorRole? authorRole,
        string? content,
        IReadOnlyList<StreamingChatToolCallUpdate>? toolCallUpdates = null,
        ChatFinishReason? completionsFinishReason = null,
        int choiceIndex = 0,
        string? modelId = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
        : base(
            authorRole,
            content,
            null,
            choiceIndex,
            modelId,
            Encoding.UTF8,
            metadata)
    {
        this.ToolCallUpdates = toolCallUpdates;
        this.FinishReason = completionsFinishReason;
    }

    /// <summary>Gets any update information in the message about a tool call.</summary>
    public IReadOnlyList<StreamingChatToolCallUpdate>? ToolCallUpdates { get; }

    /// <inheritdoc/>
    public override byte[] ToByteArray() => this.Encoding.GetBytes(this.ToString());

    /// <inheritdoc/>
    public override string ToString() => this.Content ?? string.Empty;

    private static StreamingKernelContentItemCollection CreateContentItems(IReadOnlyList<VSCodeChatMessageContentPart> contentUpdate)
    {
        StreamingKernelContentItemCollection collection = [];

        foreach (var content in contentUpdate)
        {
            // We only support text content for now.
            collection.Add(new StreamingTextContent(content.Text));
        }

        return collection;
    }
}
