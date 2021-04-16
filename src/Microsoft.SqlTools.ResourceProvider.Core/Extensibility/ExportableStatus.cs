//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ResourceProvider.Core.Extensibility
{
    /// <summary>
    /// Includes the status of the exportable - whether it failed to load and any error message
    /// returned during loading
    /// </summary>
    public class ExportableStatus
    {
        /// <summary>
        /// Returns true if the loading of the exportable failed
        /// </summary>
        public bool LoadingFailed
        {
            get;
            set;
        }

        /// <summary>
        /// An error message if the loading failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// An info link to navigate to 
        /// </summary>
        public string InfoLink { get; set; }
    }
}
