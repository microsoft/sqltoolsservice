//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.IO;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    internal static class FileUtilities
    {
        private static readonly Lock PeekDefinitionTempFolderLock = new();

        internal static string PeekDefinitionTempFolder = Path.GetTempPath() + "mssql_definition"; 
        internal static string AgentNotebookTempFolder = Path.GetTempPath() + "mssql_notebooks";
        internal static bool PeekDefinitionTempFolderCreated = false;

        internal static string GetPeekDefinitionTempFolder()
        {
            lock (PeekDefinitionTempFolderLock)
            {
                string tempPath;
                if (!PeekDefinitionTempFolderCreated)
                {
                    try
                    {
                        // create a private temp folder once per process so concurrent peek-definition requests share the same root
                        string tempFolder = string.Format("{0}_{1}", FileUtilities.PeekDefinitionTempFolder, Guid.NewGuid().ToString("N"));
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
        }

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

        /// <summary>
        /// Converts an OS-native absolute path to a <c>file://</c> URI string, normalising
        /// Windows-style backslash separators first. Works correctly on all platforms:
        /// Windows paths (<c>C:\path\file.sql</c>) and Unix paths (<c>/home/user/file.sql</c>)
        /// both produce well-formed <c>file:///...</c> URIs.
        /// <para>
        /// Do not use <c>new Uri(osPath).AbsoluteUri</c> as a replacement: on Linux/macOS a
        /// bare path without a scheme creates a relative <see cref="Uri"/>, and calling
        /// <c>.AbsoluteUri</c> on a relative URI throws <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        internal static string LocalPathToFileUri(string localPath)
        {
            // Use UriBuilder so the path is percent-encoded correctly (e.g. '#' or '?' in file
            // names become %23 / %3F rather than being treated as URI fragment/query separators).
            // UriBuilder.Path expects a forward-slash path; normalise Windows backslashes first.
            string p = localPath.Replace('\\', '/');
            // Ensure a leading '/' — UriBuilder requires an absolute path.
            // Windows paths like "c:/Users/..." become "/c:/Users/..." here.
            if (p.Length > 0 && p[0] != '/')
                p = "/" + p;
            var builder = new UriBuilder { Scheme = Uri.UriSchemeFile, Host = string.Empty, Path = p };
            return builder.Uri.AbsoluteUri;
        }

        /// <summary>
        /// Converts a <see cref="Uri"/> with <see cref="Uri.IsFile"/> == true to an OS-native
        /// absolute path, stripping the spurious leading '/' that some .NET runtimes return from
        /// <see cref="Uri.LocalPath"/> on Windows (e.g. "/c:/Users/..." → "c:/Users/...").
        /// </summary>
        internal static string UriToLocalPath(Uri uri)
        {
            string localPath = uri.LocalPath;
            // On Windows, Uri.LocalPath can start with "/c:/" — strip the leading slash.
            int start = (localPath.Length >= 3 && localPath[0] == '/' &&
                         char.IsLetter(localPath[1]) && localPath[2] == ':') ? 1 : 0;
            return localPath.Substring(start);
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