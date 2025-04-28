//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SqlTools.Connectors.VSCode;

#pragma warning disable IDE0039 // Use local function

namespace Microsoft.SemanticKernel;

/// <summary>
/// Sponsor extensions class for <see cref="IKernelBuilder"/>.
/// </summary>
public static class VSCodeKernelBuilderExtensions
{
    #region Chat Completion
    /// <summary>
    /// Adds the OpenAI chat completion service to the list.
    /// </summary>
    /// <param name="builder">The <see cref="IKernelBuilder"/> instance to augment.</param>
    /// <param name="languageModelEndpoint">Language model endpoint</param>
    /// <param name="serviceId">serviceId</param>
    /// <returns>The same instance as <paramref name="builder"/>.</returns>
    public static IKernelBuilder AddVSCodeChatCompletion(
        this IKernelBuilder builder,
        ILanguageModelEndpoint languageModelEndpoint,
        string? serviceId = null)
    {
        Verify.NotNull(builder);
        Verify.NotNull(languageModelEndpoint);

        VSCodeChatCompletionService Factory(IServiceProvider serviceProvider, object? _) => new(languageModelEndpoint);

        builder.Services.AddKeyedSingleton<IChatCompletionService>(serviceId, (Func<IServiceProvider, object?, VSCodeChatCompletionService>)Factory);

        return builder;
    }

    #endregion
}
