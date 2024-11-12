//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Diagnostics.CodeAnalysis;
using OpenAI.Chat;

namespace Microsoft.SqlTools.Connectors.VSCode;

/// <summary>
/// Extension methods for <see cref="IReadOnlyKernelPluginCollection"/>.
/// </summary>
public static class VSCodePluginCollectionExtensions
{
    /// <summary>
    /// Given an <see cref="VSCodeFunctionToolCall"/> object, tries to retrieve the corresponding <see cref="KernelFunction"/> and populate <see cref="KernelArguments"/> with its parameters.
    /// </summary>
    /// <param name="plugins">The plugins.</param>
    /// <param name="functionToolCall">The <see cref="VSCodeFunctionToolCall"/> object.</param>
    /// <param name="function">When this method returns, the function that was retrieved if one with the specified name was found; otherwise, <see langword="null"/></param>
    /// <param name="arguments">When this method returns, the arguments for the function; otherwise, <see langword="null"/></param>
    /// <returns><see langword="true"/> if the function was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFunctionAndArguments(
        this IReadOnlyKernelPluginCollection plugins,
        ChatToolCall functionToolCall,
        [NotNullWhen(true)] out KernelFunction? function,
        out KernelArguments? arguments) =>
        plugins.TryGetFunctionAndArguments(new VSCodeFunctionToolCall(functionToolCall), out function, out arguments);

    /// <summary>
    /// Given an <see cref="VSCodeFunctionToolCall"/> object, tries to retrieve the corresponding <see cref="KernelFunction"/> and populate <see cref="KernelArguments"/> with its parameters.
    /// </summary>
    /// <param name="plugins">The plugins.</param>
    /// <param name="functionToolCall">The <see cref="VSCodeFunctionToolCall"/> object.</param>
    /// <param name="function">When this method returns, the function that was retrieved if one with the specified name was found; otherwise, <see langword="null"/></param>
    /// <param name="arguments">When this method returns, the arguments for the function; otherwise, <see langword="null"/></param>
    /// <returns><see langword="true"/> if the function was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFunctionAndArguments(
        this IReadOnlyKernelPluginCollection plugins,
        VSCodeFunctionToolCall functionToolCall,
        [NotNullWhen(true)] out KernelFunction? function,
        out KernelArguments? arguments)
    {
        if (plugins.TryGetFunction(functionToolCall.PluginName, functionToolCall.FunctionName, out function))
        {
            // Add parameters to arguments
            arguments = null;
            if (functionToolCall.Arguments is not null)
            {
                arguments = [];
                foreach (var parameter in functionToolCall.Arguments)
                {
                    arguments[parameter.Key] = parameter.Value?.ToString();
                }
            }

            return true;
        }

        // Function not found in collection
        arguments = null;
        return false;
    }
}
