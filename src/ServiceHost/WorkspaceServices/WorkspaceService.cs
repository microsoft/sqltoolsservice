//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.WorkspaceServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.WorkspaceServices
{
    /// <summary>
    /// Class for handling requests/events that deal with the state of the workspace, including the
    /// opening and closing of files, the changing of configuration, etc.
    /// </summary>
    /// <typeparam name="TConfig">
    /// The type of the class used for serializing and deserializing the configuration. Must be the
    /// actual type of the instance otherwise deserialization will be incomplete.
    /// </typeparam>
    public class WorkspaceService<TConfig> where TConfig : class, new()
    {

        #region Singleton Instance Implementation

        private static readonly Lazy<WorkspaceService<TConfig>> instance = new Lazy<WorkspaceService<TConfig>>(() => new WorkspaceService<TConfig>());

        public static WorkspaceService<TConfig> Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Default, parameterless constructor.
        /// TODO: Figure out how to make this truely singleton even with dependency injection for tests
        /// </summary>
        public WorkspaceService()
        {
            ConfigChangeCallbacks = new List<ConfigChangeCallback>();
            TextDocChangeCallbacks = new List<TextDocChangeCallback>();
        }

        #endregion

        #region Properties

        public Workspace Workspace { get; private set; }

        public TConfig CurrentSettings { get; private set; }

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
        /// List of callbacks to call when the configuration of the workspace changes
        /// </summary>
        private List<ConfigChangeCallback> ConfigChangeCallbacks { get; set; }

        /// <summary>
        /// List of callbacks to call when the current text document changes
        /// </summary>
        private List<TextDocChangeCallback> TextDocChangeCallbacks { get; set; }

        #endregion

        #region Public Methods

        public void InitializeService(ServiceHost serviceHost)
        {
            // Create a workspace that will handle state for the session
            Workspace = new Workspace();
            CurrentSettings = new TConfig();

            // Register the handlers for when changes to the workspae occur
            serviceHost.SetEventHandler(DidChangeTextDocumentNotification.Type, HandleDidChangeTextDocumentNotification);
            serviceHost.SetEventHandler(DidOpenTextDocumentNotification.Type, HandleDidOpenTextDocumentNotification);
            serviceHost.SetEventHandler(DidCloseTextDocumentNotification.Type, HandleDidCloseTextDocumentNotification);
            serviceHost.SetEventHandler(DidChangeConfigurationNotification<TConfig>.Type, HandleDidChangeConfigurationNotification);
            
            // Register an initialization handler that sets the workspace path
            serviceHost.RegisterInitializeTask(async (parameters, contect) =>
            {
                Logger.Write(LogLevel.Verbose, "Initializing workspace service");

                if (Workspace != null)
                {
                    Workspace.WorkspacePath = parameters.RootPath;
                }
                await Task.FromResult(0);
            });

            // Register a shutdown request that disposes the workspace
            serviceHost.RegisterShutdownTask(async (parameters, context) =>
            {
                Logger.Write(LogLevel.Verbose, "Shutting down workspace service");

                if (Workspace != null)
                {
                    Workspace.Dispose();
                    Workspace = null;
                }
                await Task.FromResult(0);
            });
        }

        /// <summary>
        /// Adds a new task to be called when the configuration has been changed. Use this to
        /// handle changing configuration and changing the current configuration.
        /// </summary>
        /// <param name="task">Task to handle the request</param>
        public void RegisterConfigChangeCallback(ConfigChangeCallback task)
        {
            ConfigChangeCallbacks.Add(task);
        }

        /// <summary>
        /// Adds a new task to be called when the text of a document changes.
        /// </summary>
        /// <param name="task">Delegate to call when the document changes</param>
        public void RegisterTextDocChangeCallback(TextDocChangeCallback task)
        {
            TextDocChangeCallbacks.Add(task);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles text document change events
        /// </summary>
        protected Task HandleDidChangeTextDocumentNotification(
            DidChangeTextDocumentParams textChangeParams,
            EventContext eventContext)
        {
            StringBuilder msg = new StringBuilder();
            msg.Append("HandleDidChangeTextDocumentNotification");
            List<ScriptFile> changedFiles = new List<ScriptFile>();

            // A text change notification can batch multiple change requests
            foreach (var textChange in textChangeParams.ContentChanges)
            {
                string fileUri = textChangeParams.TextDocument.Uri;
                msg.AppendLine(String.Format("  File: {0}", fileUri));

                ScriptFile changedFile = Workspace.GetFile(fileUri);

                changedFile.ApplyChange(
                    GetFileChangeDetails(
                        textChange.Range.Value,
                        textChange.Text));

                changedFiles.Add(changedFile);
            }

            Logger.Write(LogLevel.Verbose, msg.ToString());

            var handlers = TextDocChangeCallbacks.Select(t => t(changedFiles.ToArray(), eventContext));
            return Task.WhenAll(handlers);
        }

        protected Task HandleDidOpenTextDocumentNotification(
            DidOpenTextDocumentNotification openParams,
            EventContext eventContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDidOpenTextDocumentNotification");
            return Task.FromResult(true);
        }

        protected Task HandleDidCloseTextDocumentNotification(
           TextDocumentIdentifier closeParams,
           EventContext eventContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDidCloseTextDocumentNotification");
            return Task.FromResult(true);
        }

        /// <summary>
        /// Handles the configuration change event
        /// </summary>
        protected async Task HandleDidChangeConfigurationNotification(
            DidChangeConfigurationParams<TConfig> configChangeParams,
            EventContext eventContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDidChangeConfigurationNotification");

            // Propagate the changes to the event handlers
            var configUpdateTasks = ConfigChangeCallbacks.Select(
                t => t(configChangeParams.Settings, CurrentSettings, eventContext));
            await Task.WhenAll(configUpdateTasks);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Switch from 0-based offsets to 1 based offsets
        /// </summary>
        /// <param name="changeRange"></param>
        /// <param name="insertString"></param>       
        private static FileChange GetFileChangeDetails(Range changeRange, string insertString)
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

        #endregion
    }
}
