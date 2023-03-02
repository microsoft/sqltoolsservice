//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Defines the common settings used by the tools service
    /// </summary>
    public interface ISqlToolsSettingsValues
    {
        /// <summary>
        /// Intellisense specific settings
        /// </summary>
        IntelliSenseSettings IntelliSense { get; set; }

        /// <summary>
        /// Query execution specific settings
        /// </summary>
        QueryExecutionSettings QueryExecutionSettings { get; set; }

        /// <summary>
        /// Formatter settings
        /// </summary>
        FormatterSettings Format { get; set; }

        /// <summary>
        /// Object Explorer specific settings
        /// </summary>
        ObjectExplorerSettings ObjectExplorer { get; set; }

        /// <summary>
        /// PII Logging setting
        /// </summary>
        Boolean PiiLogging { get; set; }
    }
}