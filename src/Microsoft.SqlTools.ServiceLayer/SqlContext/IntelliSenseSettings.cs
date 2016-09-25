//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
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
            this.EnableSuggestions = true;
            this.LowerCaseSuggestions = false;
            this.EnableDiagnostics = true;
        }

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
        public bool? EnableDiagnostics { get; set; }

        /// <summary>
        /// Update the Intellisense settings
        /// </summary>
        /// <param name="settings"></param>
        public void Update(IntelliSenseSettings settings)
        {
            if (settings != null)
            {
                this.EnableSuggestions = settings.EnableSuggestions;
                this.LowerCaseSuggestions = settings.LowerCaseSuggestions;
                this.EnableDiagnostics = settings.EnableDiagnostics;
            }
        }
    }
}
