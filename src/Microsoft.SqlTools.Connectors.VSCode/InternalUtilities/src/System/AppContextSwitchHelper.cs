//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SemanticKernel;

/// <summary>
/// Helper class to get app context switch value
/// </summary>
[ExcludeFromCodeCoverage]
internal static class AppContextSwitchHelper
{
    /// <summary>
    /// Returns the value of the specified app switch or environment variable if it is set.
    /// If the switch or environment variable is not set, return false.
    /// The app switch value takes precedence over the environment variable.
    /// </summary>
    /// <param name="appContextSwitchName">The name of the app switch.</param>
    /// <param name="envVarName">The name of the environment variable.</param>
    /// <returns>The value of the app switch or environment variable if it is set; otherwise, false.</returns>
    public static bool GetConfigValue(string appContextSwitchName, string envVarName)
    {
        if (AppContext.TryGetSwitch(appContextSwitchName, out bool value))
        {
            return value;
        }

        string? envVarValue = Environment.GetEnvironmentVariable(envVarName);
        if (envVarValue != null && bool.TryParse(envVarValue, out value))
        {
            return value;
        }

        return false;
    }
}
