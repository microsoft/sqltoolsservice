//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System.Collections.Concurrent;

namespace Microsoft.SqlTools.ServiceLayer.Workspace
{
    /// <summary>
    /// Manages a "workspace" of script files that are open for a particular
    /// editing session.  Also helps to navigate references between ScriptFiles.
    /// </summary>
    public partial class Workspace : IDisposable
    {
        #region Private Fields

        private const string UntitledScheme = "untitled";

        private ConcurrentDictionary<string, ScriptFile> workspaceFiles = new ConcurrentDictionary<string, ScriptFile>();

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
                using (var fileStream = new FileStream(resolvedFile.FilePath, FileMode.Open, FileAccess.Read))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    scriptFile = new ScriptFile(resolvedFile.FilePath, resolvedFile.ClientUri,streamReader);

                    this.workspaceFiles.TryAdd(keyName, scriptFile);
                }

                Logger.Verbose("Opened file on disk: " + resolvedFile.FilePath);
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
                if (Uri.TryCreate(clientUri, UriKind.Absolute, out Uri fileUri)
                    && fileUri.IsFile)
                {
                    // Client sent a file URI identifier, resolve to a local filesystem path.
                    filePath = fileUri.LocalPath;
                }

                // Get the absolute file path
                ResolvedFile resolvedFile = FileUtilities.TryGetFullPath(filePath, clientUri);
                filePath = resolvedFile.FilePath;
                canReadFromDisk = resolvedFile.CanReadFromDisk;
            }

            Logger.Verbose("Resolved path: " + clientUri);

            return new ResolvedFile(filePath, clientUri, canReadFromDisk);
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

            // Resolve the full file path 
            ResolvedFile resolvedFile = this.ResolveFilePath(filePath);
            string keyName = resolvedFile.LowercaseClientUri;

            // Make sure the file isn't already loaded into the workspace
            ScriptFile scriptFile = null;
            if (!this.workspaceFiles.TryGetValue(keyName, out scriptFile))
            {
                scriptFile = new ScriptFile(resolvedFile.FilePath, resolvedFile.ClientUri, initialBuffer);

                this.workspaceFiles.TryAdd(keyName, scriptFile);

                Logger.Verbose("Opened file as in-memory buffer: " + resolvedFile.FilePath);
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

            this.workspaceFiles.TryRemove(scriptFile.Id, out _);
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
