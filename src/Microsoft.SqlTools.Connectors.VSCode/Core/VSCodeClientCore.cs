// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Chat;

#pragma warning disable CA2208 // Instantiate argument exceptions correctly

namespace Microsoft.SqlTools.Connectors.VSCode;

/// <summary>
/// Language model chat completion object
/// </summary>
public class LanguageModelChatCompletion
{
    /// <summary>
    /// Result text
    /// </summary>
    public string? ResultText { get; set; }

    /// <summary>
    /// ContentUpdate
    /// </summary>
    public List<VSCodeChatMessageContentPart>? ContentUpdate => this.Content;

    /// <summary>
    /// ToolCallUpdates
    /// </summary>
    public List<StreamingChatToolCallUpdate>? ToolCallUpdates { get; set; }

    /// <summary>
    /// Role of the author of the message
    /// </summary>
    public AuthorRole? Role { get; set; }

    /// <summary>
    /// The reason why the completion finished.
    /// </summary>
    public ChatFinishReason? FinishReason { get; set; }

    /// <summary>
    /// ToolCalls
    /// </summary>
    public List<ChatToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Content
    /// </summary>
    public List<VSCodeChatMessageContentPart>? Content { get; set; }

    /// <summary>
    /// ResponseTool
    /// </summary>
    public ChatTool? ResponseTool { get; set; }

    /// <summary>
    /// ResponseToolParameters
    /// </summary>
    public string? ResponseToolParameters { get; set; }
}

public interface IVSCodeStreamResult<T>
{
    IAsyncEnumerator<LanguageModelChatCompletion> GetAsyncEnumerator(CancellationToken cancellationToken = default);
}

/// <summary>
/// Language model endpoint
/// </summary>
public interface ILanguageModelEndpoint
{
    /// <summary>
    /// Sends a request to the language model
    /// </summary>
    /// <param name="chatHistory"></param>
    /// <param name="tools"></param>
    LanguageModelChatCompletion SendChatRequest(ChatHistory chatHistory, IList<ChatTool> tools);

    /// <summary>
    /// Sends a request and streams results
    /// </summary>
    /// <param name="chatHistory"></param>
    /// <param name="tools"></param>
    /// <returns></returns>
    Task<IVSCodeStreamResult<LanguageModelChatCompletion>> SendChatRequestStreamingAsync(ChatHistory chatHistory, IList<ChatTool> tools);
}

/// <summary>
/// Base class for AI clients that provides common functionality for interacting with OpenAI services.
/// </summary>
internal partial class VSCodeClientCore
{
    /// <summary>
    /// Logger instance
    /// </summary>
    protected internal ILogger? Logger { get; init; }

    private readonly ILanguageModelEndpoint _modelEndpoint;

    public IReadOnlyDictionary<string, object?>? Attributes { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VSCodeClientCore"/> class.
    /// </summary>
    /// <param name="modelEndpoint">Model name.</param>
    internal VSCodeClientCore(ILanguageModelEndpoint modelEndpoint)
    {
        this._modelEndpoint = modelEndpoint;
        this.Logger = NullLogger.Instance;
    }

    /// <summary>
    /// Invokes the specified request and handles exceptions.
    /// </summary>
    /// <typeparam name="T">Type of the response.</typeparam>
    /// <param name="request">Request to invoke.</param>
    /// <returns>Returns the response.</returns>
    protected static async Task<T> RunRequestAsync<T>(Func<Task<T>> request)
    {
        try
        {
            return await request.Invoke().ConfigureAwait(false);
        }
        catch (ClientResultException e)
        {
            throw e.ToHttpOperationException();
        }
    }

    /// <summary>
    /// Invokes the specified request and handles exceptions.
    /// </summary>
    /// <typeparam name="T">Type of the response.</typeparam>
    /// <param name="request">Request to invoke.</param>
    /// <returns>Returns the response.</returns>
    protected static T RunRequest<T>(Func<T> request)
    {
        try
        {
            return request.Invoke();
        }
        catch (ClientResultException e)
        {
            throw e.ToHttpOperationException();
        }
    }
}
