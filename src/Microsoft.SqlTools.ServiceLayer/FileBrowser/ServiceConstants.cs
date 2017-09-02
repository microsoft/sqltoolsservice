//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    /// <summary>
    /// List of services that provide file validation callback to file browser service
    /// </summary>
    public static class FileValidationServiceConstants
    {
        /// <summary>
        /// Backup
        /// </summary>
        public const string Backup = "Backup";

        /// <summary>
        /// Restore
        /// </summary>
        public const string Restore = "Restore";
    }
}