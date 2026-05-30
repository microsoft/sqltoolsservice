//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Class for serialization and deserialization of IntelliSense settings
    /// </summary>
    public class IntelliSenseSettings
    {
        public const int DefaultBindingTimeout = 500;
        public const int DefaultParserTimeout = 500;
        public const int DefaultMetadataWarmupTimeout = 2000;
        public const int DefaultMaxScriptSize = 1024 * 1024;
        public const int DefaultMetadataFailureThreshold = 3;
        public const int DefaultMetadataFailureRetryDelay = 30000;

        /// <summary>
        /// Initialize the IntelliSense settings defaults
        /// </summary>
        public IntelliSenseSettings()
        {
            this.EnableIntellisense = true;
            this.EnableSuggestions = true;
            this.EnableErrorChecking = true;
            this.EnableQuickInfo = true;
            this.BindingTimeout = DefaultBindingTimeout;
            this.ParserTimeout = DefaultParserTimeout;
            this.MetadataWarmupTimeout = DefaultMetadataWarmupTimeout;
            this.MaxScriptSize = DefaultMaxScriptSize;
            this.MetadataFailureThreshold = DefaultMetadataFailureThreshold;
            this.MetadataFailureRetryDelay = DefaultMetadataFailureRetryDelay;
        }

        /// <summary>
        /// Gets or sets a flag determining if IntelliSense is enabled
        /// </summary>
        /// <returns></returns>
        public bool EnableIntellisense { get; set; }

        /// <summary>
        /// Gets or sets a flag determining if suggestions are enabled
        /// </summary>
        /// <returns></returns>
        public bool? EnableSuggestions { get; set; }

        /// <summary>
        /// Gets or sets a flag determining if diagnostics are enabled
        /// </summary>
        public bool? EnableErrorChecking { get; set; }

        /// <summary>
        /// Gets or sets a flag determining if quick info is enabled
        /// </summary>
        public bool? EnableQuickInfo { get; set; }

        /// <summary>
        /// Gets or sets the language-service binding timeout in milliseconds.
        /// </summary>
        public int BindingTimeout { get; set; }

        /// <summary>
        /// Gets or sets the parser timeout in milliseconds.
        /// </summary>
        public int ParserTimeout { get; set; }

        /// <summary>
        /// Gets or sets the metadata warmup timeout in milliseconds.
        /// </summary>
        public int MetadataWarmupTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum script size, in characters, for semantic IntelliSense.
        /// </summary>
        public int MaxScriptSize { get; set; }

        /// <summary>
        /// Gets or sets the number of consecutive metadata failures before metadata-backed IntelliSense is temporarily degraded.
        /// </summary>
        public int MetadataFailureThreshold { get; set; }

        /// <summary>
        /// Gets or sets the metadata retry delay in milliseconds after the failure threshold is reached.
        /// </summary>
        public int MetadataFailureRetryDelay { get; set; }

        /// <summary>
        /// Update the Intellisense settings
        /// </summary>
        /// <param name="settings"></param>
        public void Update(IntelliSenseSettings settings)
        {
            if (settings != null)
            {
                this.EnableIntellisense = settings.EnableIntellisense;
                this.EnableSuggestions = settings.EnableSuggestions;
                this.EnableErrorChecking = settings.EnableErrorChecking;
                this.EnableQuickInfo = settings.EnableQuickInfo;
                this.BindingTimeout = settings.BindingTimeout;
                this.ParserTimeout = settings.ParserTimeout;
                this.MetadataWarmupTimeout = settings.MetadataWarmupTimeout;
                this.MaxScriptSize = settings.MaxScriptSize;
                this.MetadataFailureThreshold = settings.MetadataFailureThreshold;
                this.MetadataFailureRetryDelay = settings.MetadataFailureRetryDelay;
            }
        }
    }
}
