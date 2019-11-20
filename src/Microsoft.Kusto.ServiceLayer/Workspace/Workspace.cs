//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.SqlTools.Utility;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using System.Runtime.InteropServices;
using Microsoft.Kusto.ServiceLayer.Utility;
using System.Diagnostics;

namespace Microsoft.Kusto.ServiceLayer.Workspace
{
    /// <summary>
    /// Manages a "workspace" of script files that are open for a particular
    /// editing session.  Also helps to navigate references between ScriptFiles.
    /// </summary>
    public class Workspace : IDisposable
    {
        #region Private Fields

        private const string UntitledScheme = "untitled";
        private static readonly HashSet<string> fileUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        {
            "file",
            UntitledScheme,
            "tsqloutput"
        };

        private Dictionary<string, ScriptFile> workspaceFiles = new Dictionary<string, ScriptFile>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the root path of the workspace.
        /// </summary>
        public string WorkspacePath { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the Workspace class.
        /// </summary>
        public Workspace()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if a given URI is contained in a workspace 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Flag indicating if the file is tracked in workspace</returns>
        public bool ContainsFile(string filePath)
        {
            Validate.IsNotNullOrWhitespaceString("filePath", filePath);

            // Resolve the full file path 
            ResolvedFile resolvedFile = this.ResolveFilePath(filePath);
            string keyName = resolvedFile.LowercaseClientUri;

            ScriptFile scriptFile = null;
            return this.workspaceFiles.TryGetValue(keyName, out scriptFile);
        }

        /// <summary>
        /// Gets an open file in the workspace.  If the file isn't open but
        /// exists on the filesystem, load and return it. Virtual method to
        /// allow for mocking
        /// </summary>
        /// <param name="filePath">The file path at which the script resides.</param>
        /// <exception cref="FileNotFoundException">
        /// <paramref name="filePath"/> is not found.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="filePath"/> contains a null or empty string.
        /// </exception>
        public virtual ScriptFile GetFile(string filePath)
        {
            Validate.IsNotNullOrWhitespaceString("filePath", filePath);
            if (IsNonFileUri(filePath))
            {
                return null;
            }
            
            // Resolve the full file path 
            ResolvedFile resolvedFile = this.ResolveFilePath(filePath);
            string keyName = resolvedFile.LowercaseClientUri;

            // Make sure the file isn't already loaded into the workspace
            ScriptFile scriptFile = null;
            if (!this.workspaceFiles.TryGetValue(keyName, out scriptFile))
            {
                if (IsUntitled(resolvedFile.FilePath)
                    || !resolvedFile.CanReadFromDisk
                    || !File.Exists(resolvedFile.FilePath))
                {
                    // It's either not a registered untitled file, or not a valid file on disk
                    // so any attempt to read from disk will fail.
                    return null;
                }
                // This method allows FileNotFoundException to bubble up 
                // if the file isn't found.
                using (FileStream fileStream = new FileStream(resolvedFile.FilePath, FileMode.Open, FileAccess.Read))
                using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    scriptFile = new ScriptFile(resolvedFile.FilePath, resolvedFile.ClientUri,streamReader);

                    this.workspaceFiles.Add(keyName, scriptFile);
                }

                Logger.Write(TraceEventType.Verbose, "Opened file on disk: " + resolvedFile.FilePath);
            }

            return scriptFile;
        }
        
        /// <summary>
        /// Resolves a URI identifier into an actual file on disk if it exists. 
        /// </summary>
        /// <param name="clientUri">The URI identifying the file</param>
        /// <returns></returns>
        private ResolvedFile ResolveFilePath(string clientUri)
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
                ResolvedFile resolvedFile = FileUtilities.TryGetFullPath(filePath, clientUri);
                filePath = resolvedFile.FilePath;
                canReadFromDisk = resolvedFile.CanReadFromDisk;
            }

            Logger.Write(TraceEventType.Verbose, "Resolved path: " + clientUri);

            return new ResolvedFile(filePath, clientUri, canReadFromDisk);
        }
        
        /// <summary>
        /// Unescapes any escaped [, ] or space characters. Typically use this before calling a
        /// .NET API that doesn't understand PowerShell escaped chars.
        /// </summary>
        /// <param name="path">The path to unescape.</param>
        /// <returns>The path with the ` character before [, ] and spaces removed.</returns>
        public static string UnescapePath(string path)
        {
            if (!path.Contains("`"))
            {
                return path;
            }

            return Regex.Replace(path, @"`(?=[ \[\]])", "");
        }

         /// <summary>
        /// Gets a new ScriptFile instance which is identified by the given file
        /// path and initially contains the given buffer contents.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="initialBuffer"></param>
        /// <returns></returns>
        public ScriptFile GetFileBuffer(string filePath, string initialBuffer)
        {
            Validate.IsNotNullOrWhitespaceString("filePath", filePath);
            if (IsNonFileUri(filePath))
            {
                return null;
            }

            // Resolve the full file path 
            ResolvedFile resolvedFile = this.ResolveFilePath(filePath);
            string keyName = resolvedFile.LowercaseClientUri;

            // Make sure the file isn't already loaded into the workspace
            ScriptFile scriptFile = null;
            if (!this.workspaceFiles.TryGetValue(keyName, out scriptFile))
            {
                scriptFile = new ScriptFile(resolvedFile.FilePath, resolvedFile.ClientUri, initialBuffer);

                this.workspaceFiles.Add(keyName, scriptFile);

                Logger.Write(TraceEventType.Verbose, "Opened file as in-memory buffer: " + resolvedFile.FilePath);
            }

            return scriptFile;
        }

        /// <summary>
        /// Gets an array of all opened ScriptFiles in the workspace.
        /// </summary>
        /// <returns>An array of all opened ScriptFiles in the workspace.</returns>
        public ScriptFile[] GetOpenedFiles()
        {
            return workspaceFiles.Values.ToArray();
        }

        /// <summary>
        /// Closes a currently open script file with the given file path.
        /// </summary>
        /// <param name="scriptFile">The file path at which the script resides.</param>
        public void CloseFile(ScriptFile scriptFile)
        {
            Validate.IsNotNull("scriptFile", scriptFile);

            this.workspaceFiles.Remove(scriptFile.Id);
        }

        internal string GetBaseFilePath(string filePath)
        {
            if (IsPathInMemoryOrNonFileUri(filePath))
            {
                // If the file is in memory, use the workspace path
                return this.WorkspacePath;
            }

            if (!Path.IsPathRooted(filePath))
            {
                // TODO: Assert instead?
                throw new InvalidOperationException(
                    string.Format(
                        "Must provide a full path for originalScriptPath: {0}", 
                        filePath));
            }

            // Get the directory of the file path
            return Path.GetDirectoryName(filePath); 
        }

        internal string ResolveRelativeScriptPath(string baseFilePath, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            // Get the directory of the original script file, combine it
            // with the given path and then resolve the absolute file path.
            string combinedPath =
                Path.GetFullPath(
                    Path.Combine(
                        baseFilePath,
                        relativePath));

            return combinedPath;
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

        public static string GetScheme(string uri)
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
            if (match != null && match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        private bool IsNonFileUri(string path)
        {
            string scheme = GetScheme(path);
            if (!string.IsNullOrEmpty(scheme))
            {
                return !fileUriSchemes.Contains(scheme); ;
            }
            return false;
        }
        
        private bool IsUntitled(string path)
        {
            string scheme = GetScheme(path);
            if (scheme != null && scheme.Length > 0)
            {
                return string.Compare(UntitledScheme, scheme, StringComparison.OrdinalIgnoreCase) == 0;
            }
            return false;
        }

        #endregion  

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of any Runspaces that were created for the
        /// services used in this session.
        /// </summary>
        public void Dispose()
        {
        }

        #endregion
    }
}
