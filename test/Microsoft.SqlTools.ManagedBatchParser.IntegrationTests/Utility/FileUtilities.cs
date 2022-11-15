//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;

namespace Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.Utility
{
    public class FileUtilities
    {
        /// <summary>
        /// Turns off the read-only attribute for this file
        /// </summary>
        /// <param name="fullFilePath"></param>
        internal static void SetFileReadWrite(string fullFilePath)
        {
            if (!string.IsNullOrEmpty(fullFilePath) &&
                File.Exists(fullFilePath))
            {
                File.SetAttributes(fullFilePath, FileAttributes.Normal);
            }
        }
    }
}