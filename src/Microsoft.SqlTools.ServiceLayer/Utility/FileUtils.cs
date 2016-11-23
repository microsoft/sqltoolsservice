//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    internal static class FileUtils
    {
        /// <summary>
        /// Checks if file exists and swallows exceptions, if any
        /// </summary>
        /// <param name="path"> path of the file</param>
        /// <returns></returns>
        internal static bool SafeFileExists(string path)
        {
            try
            {
                return File.Exists(path);
            }
            catch (Exception)
            {
                // Swallow exception
                return false;
            }
        }

        /// <summary>
        /// Deletes a file and swallows exceptions, if any
        /// </summary>
        /// <param name="path"></param>
        internal static void SafeFileDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
                // Swallow exception, do nothing
            }
        }

    }
}