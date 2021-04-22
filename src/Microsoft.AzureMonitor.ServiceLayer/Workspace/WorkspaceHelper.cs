using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.AzureMonitor.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using Range = Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models.Range;

namespace Microsoft.AzureMonitor.ServiceLayer.Workspace
{
    public static class WorkspaceHelper
    {
        internal const string UntitledScheme = "untitled";

        /// <summary>
        /// Unescapes any escaped [, ] or space characters. Typically use this before calling a
        /// .NET API that doesn't understand PowerShell escaped chars.
        /// </summary>
        /// <param name="path">The path to unescape.</param>
        /// <returns>The path with the ` character before [, ] and spaces removed.</returns>
        private static string UnescapePath(string path)
        {
            return !path.Contains("`")
                ? path
                : Regex.Replace(path, @"`(?=[ \[\]])", "");
        }

        internal static bool IsPathInMemoryOrNonFileUri(string path)
        {
            string scheme = GetScheme(path);
            if (!string.IsNullOrEmpty(scheme))
            {
                return !scheme.Equals("file");
            }

            return false;
        }

        private static string GetScheme(string uri)
        {
            string windowsFilePattern = @"^(?:[\w]\:|\\)";
            if (Regex.IsMatch(uri, windowsFilePattern))
            {
                // Handle windows paths, these conflict with other "URI" handling
                return null;
            }

            // Match anything that starts with xyz:, as VSCode send URIs in the format untitled:, git: etc.
            string pattern = "^([a-z][a-z0-9+.-]*):";
            Match match = Regex.Match(uri, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        internal static bool IsNonFileUri(this HashSet<string> fileUriSchemes, string path)
        {
            string scheme = GetScheme(path);
            if (!string.IsNullOrEmpty(scheme))
            {
                return !fileUriSchemes.Contains(scheme);
            }

            return false;
        }

        internal static bool IsUntitled(string path)
        {
            string scheme = GetScheme(path);
            if (!string.IsNullOrEmpty(scheme))
            {
                return string.Compare(UntitledScheme, scheme, StringComparison.OrdinalIgnoreCase) == 0;
            }

            return false;
        }

        /// <summary>
        /// Resolves a URI identifier into an actual file on disk if it exists. 
        /// </summary>
        /// <param name="clientUri">The URI identifying the file</param>
        /// <returns></returns>
        internal static ResolvedFile ResolveFilePath(string clientUri)
        {
            bool canReadFromDisk = false;
            string filePath = clientUri;
            if (!IsPathInMemoryOrNonFileUri(clientUri))
            {
                if (clientUri.StartsWith(@"file://"))
                {
                    // VS Code encodes the ':' character in the drive name, which can lead to problems parsing
                    // the URI, so unencode it if present. See https://github.com/Microsoft/vscode/issues/2990
                    clientUri = clientUri.Replace("%3A/", ":/", StringComparison.OrdinalIgnoreCase);

                    // Client sent the path in URI format, extract the local path and trim
                    // any extraneous slashes
                    Uri fileUri = new Uri(clientUri);
                    filePath = fileUri.LocalPath;
                    if (filePath.StartsWith("//") || filePath.StartsWith("\\\\") || filePath.StartsWith("/"))
                    {
                        filePath = filePath.Substring(1);
                    }
                }

                // Clients could specify paths with escaped space, [ and ] characters which .NET APIs
                // will not handle.  These paths will get appropriately escaped just before being passed
                // into the SqlTools engine.
                filePath = UnescapePath(filePath);

                // Client paths are handled a bit differently because of how we currently identifiers in
                // ADS. The URI is passed around as an identifier - but for things we control like connecting
                // an editor the URI we pass in is NOT escaped fully. This is a problem for certain functionality
                // which is handled by VS Code - such as Intellise Completion - as the URI passed in there is
                // the fully escaped URI. That means we need to do some extra work to make sure that the URI values
                // are consistent.
                // So to solve that we'll make sure to unescape ALL uri's that are passed in and store that value for
                // use as an identifier (filePath will be the actual file path on disk). 
                // # and ? are still always escaped though by ADS so we need to escape those again to get them to actually
                // match
                clientUri = Uri.UnescapeDataString(UnescapePath(clientUri));
                clientUri = clientUri.Replace("#", "%23");
                clientUri = clientUri.Replace("?", "%3F");

                // switch to unix path separators on non-Windows platforms
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    filePath = filePath.Replace('\\', '/');
                    clientUri = clientUri.Replace('\\', '/');
                }

                // Get the absolute file path
                ResolvedFile resolvedFile = TryGetFullPath(filePath, clientUri);
                filePath = resolvedFile.FilePath;
                canReadFromDisk = resolvedFile.CanReadFromDisk;
            }

            Logger.Write(TraceEventType.Verbose, "Resolved path: " + clientUri);

            return new ResolvedFile(filePath, clientUri, canReadFromDisk);
        }

        /// <summary>
        /// Attempts to resolve the given filePath to an absolute path to a file on disk, 
        /// defaulting to the original filePath if that fails. 
        /// </summary>
        /// <param name="filePath">The file path to resolve</param>
        /// <param name="clientUri">The full file path URI used by the client</param>
        /// <returns></returns>
        private static ResolvedFile TryGetFullPath(string filePath, string clientUri)
        {
            try
            {
                return new ResolvedFile(Path.GetFullPath(filePath), clientUri, true);
            }
            catch (NotSupportedException)
            {
                // This is not a standard path. 
                return new ResolvedFile(filePath, clientUri, false);
            }
        }

        /// <summary>
        /// Switch from 0-based offsets to 1 based offsets
        /// </summary>
        /// <param name="changeRange"></param>
        /// <param name="insertString"></param>       
        internal static FileChange GetFileChangeDetails(Range changeRange, string insertString)
        {
            // The protocol's positions are zero-based so add 1 to all offsets
            return new FileChange
            {
                InsertString = insertString,
                Line = changeRange.Start.Line + 1,
                Offset = changeRange.Start.Character + 1,
                EndLine = changeRange.End.Line + 1,
                EndOffset = changeRange.End.Character + 1
            };
        }
        
        internal static bool IsScmEvent(string filePath)
        {
            // if the URI is prefixed with git: then we want to skip processing that file
            return filePath.StartsWith("git:");
        }
    }
}