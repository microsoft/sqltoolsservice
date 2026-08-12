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
        public const int DefaultCompletionTimeoutInMilliseconds = 2_000;

        public const int MinimumCompletionTimeoutInMilliseconds = 500;

        public const int MaximumCompletionTimeoutInMilliseconds = 30_000;

        /// <summary>
        /// Initialize the IntelliSense settings defaults
        /// </summary>
        public IntelliSenseSettings()
        {
            this.EnableIntellisense = true;
            this.EnableSuggestions = true;
            this.EnableErrorChecking = true;
            this.EnableQuickInfo = true;
            this.CompletionTimeoutMilliseconds = DefaultCompletionTimeoutInMilliseconds;
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
        /// Gets or sets the maximum time, in milliseconds, to wait for a completion operation.
        /// </summary>
        public int? CompletionTimeoutMilliseconds { get; set; }

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
                this.CompletionTimeoutMilliseconds = settings.CompletionTimeoutMilliseconds;
            }
        }
    }
}
