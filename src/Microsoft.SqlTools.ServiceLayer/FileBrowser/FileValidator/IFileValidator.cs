//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.FileValidator
{
    /// <summary>
    /// Interface to validate selected files in the file browser
    /// </summary>
    public interface IFileValidator
    {
        /// <summary>
        /// Validate selected file paths
        /// </summary>
        /// <param name="filePaths">selected file paths</param>
        /// <param name="errorMessage">error message if any of the paths is invalid</param>
        /// <returns></returns>
        bool ValidatePaths(string[] filePaths, out string errorMessage);
    }
}
