//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Hosting.DataContracts.SqlContext
{
    /// <summary>
    /// Class for serialization and deserialization of IntelliSense settings
    /// </summary>
    public class IntelliSenseSettings
    {
        /// <summary>
        /// Initialize the IntelliSense settings defaults
        /// </summary>
        public IntelliSenseSettings()
        {
            EnableIntellisense = true;
            EnableSuggestions = true;
            LowerCaseSuggestions = false;
            EnableErrorChecking = true;
            EnableQuickInfo = true;
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
        /// Gets or sets a flag determining if built-in suggestions should be lowercase
        /// </summary>
        public bool? LowerCaseSuggestions { get; set; }

        /// <summary>
        /// Gets or sets a flag determining if diagnostics are enabled
        /// </summary>
        public bool? EnableErrorChecking { get; set; }

        /// <summary>
        /// Gets or sets a flag determining if quick info is enabled
        /// </summary>
        public bool? EnableQuickInfo { get; set; }

        /// <summary>
        /// Update the Intellisense settings
        /// </summary>
        /// <param name="settings"></param>
        public void Update(IntelliSenseSettings settings)
        {
            if (settings != null)
            {
                EnableIntellisense = settings.EnableIntellisense;
                EnableSuggestions = settings.EnableSuggestions;
                LowerCaseSuggestions = settings.LowerCaseSuggestions;
                EnableErrorChecking = settings.EnableErrorChecking;
                EnableQuickInfo = settings.EnableQuickInfo;
            }
        }
    }
}
