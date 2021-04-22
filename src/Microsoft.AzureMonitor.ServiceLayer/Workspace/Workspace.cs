using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.AzureMonitor.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.Workspace
{
    public class Workspace : IDisposable
    {
        private readonly Dictionary<string, ScriptFile> _workspaceFiles;
        private readonly HashSet<string> _fileUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        {
            "file",
            WorkspaceHelper.UntitledScheme,
            "tsqloutput"
        };
        
        /// <summary>
        /// Gets or sets the root path of the workspace.
        /// </summary>
        public string WorkspacePath { get; set; }

        public Workspace()
        {
            _workspaceFiles =  new Dictionary<string, ScriptFile>();
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
        public ScriptFile GetFile(string filePath)
        {
            Validate.IsNotNullOrWhitespaceString("filePath", filePath);
            if (_fileUriSchemes.IsNonFileUri(filePath))
            {
                return null;
            }

            // Resolve the full file path 
            ResolvedFile resolvedFile = WorkspaceHelper.ResolveFilePath(filePath);
            string keyName = resolvedFile.LowercaseClientUri;

            // Make sure the file isn't already loaded into the workspace
            if (_workspaceFiles.TryGetValue(keyName, out ScriptFile scriptFile))
            {
                return scriptFile;
            }
            
            if (WorkspaceHelper.IsUntitled(resolvedFile.FilePath)
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
            {
                using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    scriptFile = new ScriptFile(resolvedFile.FilePath, resolvedFile.ClientUri, streamReader);

                    _workspaceFiles.Add(keyName, scriptFile);
                }
            }

            Logger.Write(TraceEventType.Verbose, "Opened file on disk: " + resolvedFile.FilePath);

            return scriptFile;
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
            if (_fileUriSchemes.IsNonFileUri(filePath))
            {
                return null;
            }

            // Resolve the full file path 
            ResolvedFile resolvedFile = WorkspaceHelper.ResolveFilePath(filePath);
            string keyName = resolvedFile.LowercaseClientUri;

            // Make sure the file isn't already loaded into the workspace
            if (!_workspaceFiles.TryGetValue(keyName, out ScriptFile scriptFile))
            {
                scriptFile = new ScriptFile(resolvedFile.FilePath, resolvedFile.ClientUri, initialBuffer);
                _workspaceFiles.Add(keyName, scriptFile);

                Logger.Write(TraceEventType.Verbose, "Opened file as in-memory buffer: " + resolvedFile.FilePath);
            }

            return scriptFile;
        }
        
        /// <summary>
        /// Closes a currently open script file with the given file path.
        /// </summary>
        /// <param name="scriptFile">The file path at which the script resides.</param>
        public void CloseFile(ScriptFile scriptFile)
        {
            Validate.IsNotNull("scriptFile", scriptFile);
            _workspaceFiles.Remove(scriptFile.Id);
        }

        public void Dispose()
        {
        }
    }
}