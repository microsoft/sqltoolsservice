using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.DataContracts.Workspace;
using Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.Workspace
{
    public class WorkspaceService<TConfig> where TConfig : class, new()
    {
        private static readonly Lazy<WorkspaceService<TConfig>> _instance = new Lazy<WorkspaceService<TConfig>>(() => new WorkspaceService<TConfig>());
        public static WorkspaceService<TConfig> Instance => _instance.Value;

        private Workspace _workspace;

        /// <summary>
        /// List of callbacks to call when the configuration of the workspace changes
        /// </summary>
        private readonly List<ConfigChangeCallback> _configChangeCallbacks;

        /// <summary>
        /// List of callbacks to call when the current text document changes
        /// </summary>
        private readonly List<TextDocChangeCallback> _textDocChangeCallbacks;

        /// <summary>
        /// List of callbacks to call when a text document is opened
        /// </summary>
        private readonly List<TextDocOpenCallback> _textDocOpenCallbacks;

        /// <summary>
        /// List of callbacks to call when a text document is closed
        /// </summary>
        private readonly List<TextDocCloseCallback> _textDocCloseCallbacks;

        /// <summary>
        /// Current settings for the workspace
        /// </summary>
        private readonly TConfig _currentSettings;
        
        /// <summary>
        /// Delegate for callbacks that occur when the configuration for the workspace changes
        /// </summary>
        /// <param name="newSettings">The settings that were just set</param>
        /// <param name="oldSettings">The settings before they were changed</param>
        /// <param name="eventContext">Context of the event that triggered the callback</param>
        /// <returns></returns>
        public delegate Task ConfigChangeCallback(TConfig newSettings, TConfig oldSettings, EventContext eventContext);

        /// <summary>
        /// Delegate for callbacks that occur when the current text document changes
        /// </summary>
        /// <param name="changedFiles">Array of files that changed</param>
        /// <param name="eventContext">Context of the event raised for the changed files</param>
        public delegate Task TextDocChangeCallback(ScriptFile[] changedFiles, EventContext eventContext);

        /// <summary>
        /// Delegate for callbacks that occur when a text document is opened
        /// </summary>
        /// <param name="uri">Request uri</param>
        /// <param name="openFile">File that was opened</param>
        /// <param name="eventContext">Context of the event raised for the changed files</param>
        public delegate Task TextDocOpenCallback(string uri, ScriptFile openFile, EventContext eventContext);

        /// <summary>
        /// Delegate for callbacks that occur when a text document is closed
        /// </summary>
        /// <param name="uri">Request uri</param>
        /// <param name="closedFile">File that was closed</param>
        /// <param name="eventContext">Context of the event raised for changed files</param>
        public delegate Task TextDocCloseCallback(string uri, ScriptFile closedFile, EventContext eventContext);

        public WorkspaceService()
        {
            _workspace = new Workspace();
            _configChangeCallbacks = new List<ConfigChangeCallback>();
            _textDocChangeCallbacks = new List<TextDocChangeCallback>();
            _textDocOpenCallbacks = new List<TextDocOpenCallback>();
            _textDocCloseCallbacks = new List<TextDocCloseCallback>();
            _currentSettings = new TConfig();
        }
        
        /// <summary>
        /// Adds a new task to be called when the configuration has been changed. Use this to
        /// handle changing configuration and changing the current configuration.
        /// </summary>
        /// <param name="task">Task to handle the request</param>
        public void RegisterConfigChangeCallback(ConfigChangeCallback task)
        {
            _configChangeCallbacks.Add(task);
        }

        /// <summary>
        /// Adds a new task to be called when the text of a document changes.
        /// </summary>
        /// <param name="task">Delegate to call when the document changes</param>
        public void RegisterTextDocChangeCallback(TextDocChangeCallback task)
        {
            _textDocChangeCallbacks.Add(task);
        }

        /// <summary>
        /// Adds a new task to be called when a text document closes.
        /// </summary>
        /// <param name="task">Delegate to call when the document closes</param>
        public void RegisterTextDocCloseCallback(TextDocCloseCallback task)
        {
            _textDocCloseCallbacks.Add(task);
        }

        /// <summary>
        /// Adds a new task to be called when a file is opened
        /// </summary>
        /// <param name="task">Delegate to call when a document is opened</param>
        public void RegisterTextDocOpenCallback(TextDocOpenCallback task)
        {
            _textDocOpenCallbacks.Add(task);
        }
        
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetEventHandler(DidChangeTextDocumentNotification.Type, HandleDidChangeTextDocumentNotification);
            serviceHost.SetEventHandler(DidOpenTextDocumentNotification.Type, HandleDidOpenTextDocumentNotification);
            serviceHost.SetEventHandler(DidCloseTextDocumentNotification.Type, HandleDidCloseTextDocumentNotification);
            serviceHost.SetEventHandler(DidChangeConfigurationNotification<TConfig>.Type, HandleDidChangeConfigurationNotification);

            // Register an initialization handler that sets the workspace path
            serviceHost.RegisterInitializeTask(InitializeWorkspace);

            // Register a shutdown request that disposes the workspace
            serviceHost.RegisterShutdownTask(ShutdownWorkspace);
        }

        private Task InitializeWorkspace(InitializeRequest startupParams, RequestContext<InitializeResult> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "Initializing workspace service");

            if (_workspace != null)
            {
                _workspace.WorkspacePath = startupParams.RootPath;
            }

            return Task.FromResult(0);
        }

        private Task ShutdownWorkspace(object shutdownParams, RequestContext<object> shutdownRequestContext)
        {
            Logger.Write(TraceEventType.Verbose, "Shutting down workspace service");

            if (_workspace != null)
            {
                _workspace.Dispose();
                _workspace = null;
            }
            return Task.FromResult(0);
        }

        private Task HandleDidChangeTextDocumentNotification(DidChangeTextDocumentParams textChangeParams, EventContext eventContext)
        {
            try
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("HandleDidChangeTextDocumentNotification");
                var changedFiles = new List<ScriptFile>();
                string fileUri = textChangeParams.TextDocument.Uri;
                
                // A text change notification can batch multiple change requests
                foreach (TextDocumentChangeEvent textChange in textChangeParams.ContentChanges)
                {
                    stringBuilder.AppendLine($"  File: {fileUri}");

                    ScriptFile changedFile = _workspace.GetFile(fileUri);
                    if (changedFile != null)
                    {
                        var fileChange = WorkspaceHelper.GetFileChangeDetails(textChange.Range.Value, textChange.Text);
                        changedFile.ApplyChange(fileChange);
                        changedFiles.Add(changedFile);
                    }
                }

                Logger.Write(TraceEventType.Verbose, stringBuilder.ToString());

                var handlers = _textDocChangeCallbacks.Select(t => t(changedFiles.ToArray(), eventContext));
                return Task.WhenAll(handlers);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex);
                // Swallow exceptions here to prevent us from crashing
                // TODO: this probably means the ScriptFile model is in a bad state or out of sync with the actual file; we should recover here
                return Task.FromResult(true);
            }
        }

        private async Task HandleDidOpenTextDocumentNotification(DidOpenTextDocumentNotification openParams, EventContext eventContext)
        {
            try
            {
                Logger.Write(TraceEventType.Verbose, "HandleDidOpenTextDocumentNotification");

                if (WorkspaceHelper.IsScmEvent(openParams.TextDocument.Uri))
                {
                    return;
                }

                // read the SQL file contents into the ScriptFile 
                ScriptFile openedFile = _workspace.GetFileBuffer(openParams.TextDocument.Uri, openParams.TextDocument.Text);
                if (openedFile == null)
                {
                    return;
                }
                // Propagate the changes to the event handlers
                var handlers = _textDocOpenCallbacks.Select(t => t(openParams.TextDocument.Uri, openedFile, eventContext));
                await Task.WhenAll(handlers);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex);
                // Swallow exceptions here to prevent us from crashing
                // TODO: this probably means the ScriptFile model is in a bad state or out of sync with the actual file; we should recover here
            }
        }

        private async Task HandleDidCloseTextDocumentNotification(DidCloseTextDocumentParams closeParams, EventContext eventContext)
        {
            try
            {
                Logger.Write(TraceEventType.Verbose, "HandleDidCloseTextDocumentNotification");

                if (WorkspaceHelper.IsScmEvent(closeParams.TextDocument.Uri)) 
                {
                    return;
                }

                // Skip closing this file if the file doesn't exist
                var closedFile = _workspace.GetFile(closeParams.TextDocument.Uri);
                if (closedFile == null)
                {
                    return;
                }

                // Trash the existing document from our mapping
                _workspace.CloseFile(closedFile);

                // Send out a notification to other services that have subscribed to this event
                var handlers = _textDocCloseCallbacks.Select(t => t(closeParams.TextDocument.Uri, closedFile, eventContext));
                await Task.WhenAll(handlers);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex);
                // Swallow exceptions here to prevent us from crashing
                // TODO: this probably means the ScriptFile model is in a bad state or out of sync with the actual file; we should recover here
            }
        }

        private async Task HandleDidChangeConfigurationNotification(DidChangeConfigurationParams<TConfig> configChangeParams, EventContext eventContext)
        {
            try
            {
                Logger.Write(TraceEventType.Verbose, "HandleDidChangeConfigurationNotification");

                // Propagate the changes to the event handlers
                var handlers = _configChangeCallbacks.Select(t => t(configChangeParams.Settings, _currentSettings, eventContext));
                await Task.WhenAll(handlers);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex);
                // Swallow exceptions here to prevent us from crashing
                // TODO: this probably means the ScriptFile model is in a bad state or out of sync with the actual file; we should recover here
            }
        }

        public string GetSqlTextFromSelectionData(string ownerUri, SelectionData selection)
        {
            // Get the document from the parameters
            ScriptFile queryFile = _workspace.GetFile(ownerUri);
            if (queryFile == null)
            {
                return string.Empty;
            }
            // If a selection was not provided, use the entire document
            if (selection == null)
            {
                return queryFile.Contents;
            }

            // A selection was provided, so get the lines in the selected range

            var bufferRange = new BufferRange(
                new BufferPosition(
                    selection.StartLine + 1,
                    selection.StartColumn + 1
                ),
                new BufferPosition(
                    selection.EndLine + 1,
                    selection.EndColumn + 1
                )
            ); 
            
            string[] queryTextArray = queryFile.GetLinesInRange(bufferRange);
            return string.Join(Environment.NewLine, queryTextArray);
        }
    }
}