//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SqlTools.Connectors.VSCode;

namespace Microsoft.SemanticKernel;

#pragma warning disable IDE0039 // Use local function

/// <summary>
/// Sponsor extensions class for <see cref="IServiceCollection"/>.
/// </summary>
public static class VSCodeServiceCollectionExtensions
{
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
    #endregion
}
