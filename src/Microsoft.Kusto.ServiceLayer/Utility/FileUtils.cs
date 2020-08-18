//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
namespace Microsoft.Kusto.ServiceLayer.Utility
{
    internal static class FileUtilities
    {
        internal static string PeekDefinitionTempFolder = Path.GetTempPath() + "mssql_definition"; 
        internal static string AgentNotebookTempFolder = Path.GetTempPath() + "mssql_notebooks";
        internal static bool PeekDefinitionTempFolderCreated = false;

        internal static string GetPeekDefinitionTempFolder()
        {
            string tempPath;
            if (!PeekDefinitionTempFolderCreated)
            {               
                try
                {
                    // create new temp folder
                    string tempFolder = string.Format("{0}_{1}", FileUtilities.PeekDefinitionTempFolder, DateTime.Now.ToString("yyyyMMddHHmmssffff"));
                    DirectoryInfo tempScriptDirectory = Directory.CreateDirectory(tempFolder);
                    FileUtilities.PeekDefinitionTempFolder = tempScriptDirectory.FullName;
                    tempPath = tempScriptDirectory.FullName;
                    PeekDefinitionTempFolderCreated = true;
                }
                catch (Exception)
                {
                    // swallow exception and use temp folder to store scripts
                    tempPath = Path.GetTempPath();
                }
            }
            else
            {
                try
                {
                    // use tempDirectory name created previously
                    DirectoryInfo tempScriptDirectory = Directory.CreateDirectory(FileUtilities.PeekDefinitionTempFolder);
                    tempPath = tempScriptDirectory.FullName;
                }
                catch (Exception)
                {
                    // swallow exception and use temp folder to store scripts
                    tempPath = Path.GetTempPath();
                }
            }
            return tempPath;
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

        internal static int WriteWithLength(Stream stream, byte[] buffer, int length)
        {
            stream.Write(buffer, 0, length);
            return length;
        }

        /// <summary>
        /// Checks if file exists and swallows exceptions, if any
        /// </summary>
        /// <param name="path"> path of the file</param>
        /// <returns></returns>
        internal static bool SafeDirectoryExists(string path)
        {
            try
            {
                return Directory.Exists(path);
            }
            catch (Exception)
            {
                // Swallow exception
                return false;
            }
        }


        /// <summary>
        /// Deletes a directory and swallows exceptions, if any
        /// </summary>
        /// <param name="path"></param>
        internal static void SafeDirectoryDelete(string path, bool recursive)
        {
            try
            {
                Directory.Delete(path, recursive);
            }
            catch (Exception)
            {
                // Swallow exception, do nothing
            }
        }

        /// <summary>
        /// Attempts to resolve the given filePath to an absolute path to a file on disk, 
        /// defaulting to the original filePath if that fails. 
        /// </summary>
        /// <param name="filePath">The file path to resolve</param>
        /// <param name="clientUri">The full file path URI used by the client</param>
        /// <returns></returns>
        internal static ResolvedFile TryGetFullPath(string filePath, string clientUri)
        {
            try
            {
                return new ResolvedFile(Path.GetFullPath(filePath), clientUri, true);
            }
            catch(NotSupportedException)
            {
                // This is not a standard path. 
                return new ResolvedFile(filePath, clientUri, false);
            }
        }
    }
}