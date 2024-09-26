// Copyright (c) Microsoft. All rights reserved.

// using System;
using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
//using OpenAI;

#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
#pragma warning disable RCS1155 // Use StringComparison when comparing strings

namespace Microsoft.SqlTools.Connectors.VSCode;

/// <summary>
/// OpenAI chat completion service.
/// </summary>
public sealed class VSCodeChatCompletionService : IChatCompletionService //, ITextGenerationService
{
    /// <summary>Core implementation shared by OpenAI clients.</summary>
    private readonly VSCodeClientCore _client;

    /// <summary>
    /// Create an instance of the OpenAI chat completion connector
    /// </summary>
    /// <param name="languageModelEndpoint">languageModelEndpoint</param>
    public VSCodeChatCompletionService(
        ILanguageModelEndpoint languageModelEndpoint)
    {
        this._client = new(languageModelEndpoint);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => this._client.Attributes;

    /// <inheritdoc/>
    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => this._client.GetChatMessageContentsAsync("this._client.ModelId", chatHistory, executionSettings, kernel, cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => this._client.GetStreamingChatMessageContentsAsync("this._client.ModelId", chatHistory, executionSettings, kernel, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => this._client.GetChatAsTextContentsAsync("this._client.ModelId", prompt, executionSettings, kernel, cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => this._client.GetChatAsTextStreamingContentsAsync("this._client.ModelId", prompt, executionSettings, kernel, cancellationToken);
}
