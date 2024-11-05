// Copyright (c) Microsoft. All rights reserved.

using System;
//using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
// using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.ChatCompletion;
// using Microsoft.SemanticKernel.Embeddings;
//using Microsoft.SemanticKernel.Http;
//using Microsoft.SemanticKernel.TextGeneration;
// using Microsoft.SemanticKernel.TextToAudio;
// using Microsoft.SemanticKernel.TextToImage;
using Microsoft.SqlTools.Connectors.VSCode;
//using OpenAI;

namespace Microsoft.SemanticKernel;

#pragma warning disable IDE0039 // Use local function

/// <summary>
/// Sponsor extensions class for <see cref="IServiceCollection"/>.
/// </summary>
public static class VSCodeServiceCollectionExtensions
{
#if false
    #region Text Embedding
    /// <summary>
    /// Adds the <see cref="OpenAITextEmbeddingGenerationService"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="modelId">OpenAI model name, see https://platform.openai.com/docs/models</param>
    /// <param name="apiKey">OpenAI API key, see https://platform.openai.com/account/api-keys</param>
    /// <param name="orgId">OpenAI organization id. This is usually optional unless your account belongs to multiple organizations.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <param name="dimensions">The number of dimensions the resulting output embeddings should have. Only supported in "text-embedding-3" and later models.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    [Experimental("SKEXP0010")]
    public static IServiceCollection AddOpenAITextEmbeddingGeneration(
        this IServiceCollection services,
        string modelId,
        string apiKey,
        string? orgId = null,
        string? serviceId = null,
        int? dimensions = null)
    {
        Verify.NotNull(services);
        Verify.NotNullOrWhiteSpace(modelId);
        Verify.NotNullOrWhiteSpace(apiKey);

        return services.AddKeyedSingleton<ITextEmbeddingGenerationService>(serviceId, (serviceProvider, _) =>
            new OpenAITextEmbeddingGenerationService(
                modelId,
                apiKey,
                orgId,
                HttpClientProvider.GetHttpClient(serviceProvider),
                serviceProvider.GetService<ILoggerFactory>(),
                dimensions));
    }

    /// <summary>
    /// Adds the <see cref="OpenAITextEmbeddingGenerationService"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="modelId">The OpenAI model id.</param>
    /// <param name="openAIClient"><see cref="OpenAIClient"/> to use for the service. If null, one must be available in the service provider when this service is resolved.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <param name="dimensions">The number of dimensions the resulting output embeddings should have. Only supported in "text-embedding-3" and later models.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    [Experimental("SKEXP0010")]
    public static IServiceCollection AddOpenAITextEmbeddingGeneration(this IServiceCollection services,
        string modelId,
        OpenAIClient? openAIClient = null,
        string? serviceId = null,
        int? dimensions = null)
    {
        Verify.NotNull(services);
        Verify.NotNullOrWhiteSpace(modelId);

        return services.AddKeyedSingleton<ITextEmbeddingGenerationService>(serviceId, (serviceProvider, _) =>
            new OpenAITextEmbeddingGenerationService(
                modelId,
                openAIClient ?? serviceProvider.GetRequiredService<OpenAIClient>(),
                serviceProvider.GetService<ILoggerFactory>(),
                dimensions));
    }
    #endregion
#endif

    #region Chat Completion

    /// <summary>
    /// Adds the OpenAI chat completion service to the list.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="languageModelEndpoint">languageModelEndpoint</param>
    /// <param name="serviceId">serviceId</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection AddVSCodeChatCompletion(
        this IServiceCollection services,
        ILanguageModelEndpoint languageModelEndpoint,
        string? serviceId = null)
    {
        Verify.NotNull(services);
        Verify.NotNull(languageModelEndpoint);

        VSCodeChatCompletionService Factory(IServiceProvider serviceProvider, object? _) => new(languageModelEndpoint);

        services.AddKeyedSingleton<IChatCompletionService>(serviceId, (Func<IServiceProvider, object?, VSCodeChatCompletionService>)Factory);

        // services.AddKeyedSingleton<ITextGenerationService>(serviceId, (Func<IServiceProvider, object?, OpenAIChatCompletionService>)Factory);

        return services;
    }

#if false

    /// <summary>
    /// Adds the OpenAI chat completion service to the list.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="modelId">OpenAI model id</param>
    /// <param name="openAIClient"><see cref="OpenAIClient"/> to use for the service. If null, one must be available in the service provider when this service is resolved.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection AddOpenAIChatCompletion(this IServiceCollection services,
        string modelId,
        OpenAIClient? openAIClient = null,
        string? serviceId = null)
    {
        Verify.NotNull(services);
        Verify.NotNullOrWhiteSpace(modelId);

        VSCodeChatCompletionService Factory(IServiceProvider serviceProvider, object? _) =>
            new(modelId, openAIClient ?? serviceProvider.GetRequiredService<OpenAIClient>(), serviceProvider.GetService<ILoggerFactory>());

        services.AddKeyedSingleton<IChatCompletionService>(serviceId, (Func<IServiceProvider, object?, VSCodeChatCompletionService>)Factory);

        // services.AddKeyedSingleton<ITextGenerationService>(serviceId, (Func<IServiceProvider, object?, OpenAIChatCompletionService>)Factory);

        return services;
    }

    /// <summary>
    /// Adds the Custom OpenAI chat completion service to the list.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="modelId">OpenAI model name, see https://platform.openai.com/docs/models</param>
    /// <param name="endpoint">A Custom Message API compatible endpoint.</param>
    /// <param name="apiKey">OpenAI API key, see https://platform.openai.com/account/api-keys</param>
    /// <param name="orgId">OpenAI organization id. This is usually optional unless your account belongs to multiple organizations.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    [Experimental("SKEXP0010")]
    public static IServiceCollection AddOpenAIChatCompletion(
        this IServiceCollection services,
        string modelId,
        Uri endpoint,
        string? apiKey = null,
        string? orgId = null,
        string? serviceId = null)
    {
        Verify.NotNull(services);
        Verify.NotNullOrWhiteSpace(modelId);

        VSCodeChatCompletionService Factory(IServiceProvider serviceProvider, object? _) =>
            new(modelId,
                endpoint,
                apiKey,
                orgId,
                HttpClientProvider.GetHttpClient(serviceProvider),
                serviceProvider.GetService<ILoggerFactory>());

        services.AddKeyedSingleton<IChatCompletionService>(serviceId, (Func<IServiceProvider, object?, VSCodeChatCompletionService>)Factory);

        // services.AddKeyedSingleton<ITextGenerationService>(serviceId, (Func<IServiceProvider, object?, OpenAIChatCompletionService>)Factory);

        return services;
    }
#endif
    #endregion
}
