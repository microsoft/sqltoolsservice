﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using Range = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Range;

namespace Microsoft.SqlTools.ServiceLayer.Workspace
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

        private static Lazy<WorkspaceService<TConfig>> instance = new Lazy<WorkspaceService<TConfig>>(() => new WorkspaceService<TConfig>());

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
            TextDocOpenCallbacks = new List<TextDocOpenCallback>();
            TextDocCloseCallbacks = new List<TextDocCloseCallback>();

            CurrentSettings = new TConfig();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Workspace object for the service. Virtual to allow for mocking
        /// </summary>
        public virtual Workspace Workspace { get; internal set; }

        /// <summary>
        /// Current settings for the workspace
        /// </summary>
        public TConfig CurrentSettings { get; internal set; }

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

        /// <summary>
        /// List of callbacks to call when the configuration of the workspace changes
        /// </summary>
        private List<ConfigChangeCallback> ConfigChangeCallbacks { get; set; }

        /// <summary>
        /// List of callbacks to call when the current text document changes
        /// </summary>
        private List<TextDocChangeCallback> TextDocChangeCallbacks { get; set; }

        /// <summary>
        /// List of callbacks to call when a text document is opened
        /// </summary>
        private List<TextDocOpenCallback> TextDocOpenCallbacks { get; set; }

        /// <summary>
        /// List of callbacks to call when a text document is closed
        /// </summary>
        private List<TextDocCloseCallback> TextDocCloseCallbacks { get; set; }
 

        #endregion

        #region Public Methods

        public void InitializeService(ServiceHost serviceHost)
        {
            // Create a workspace that will handle state for the session
            Workspace = new Workspace();

            // Register the handlers for when changes to the workspae occur
            serviceHost.SetEventHandler(DidChangeTextDocumentNotification.Type, HandleDidChangeTextDocumentNotification);
            serviceHost.SetEventHandler(DidOpenTextDocumentNotification.Type, HandleDidOpenTextDocumentNotification);
            serviceHost.SetEventHandler(DidCloseTextDocumentNotification.Type, HandleDidCloseTextDocumentNotification);
            serviceHost.SetEventHandler(DidChangeConfigurationNotification<TConfig>.Type, HandleDidChangeConfigurationNotification);
            
            // Register an initialization handler that sets the workspace path
            serviceHost.RegisterInitializeTask(async (parameters, contect) =>
            {
                Logger.Write(TraceEventType.Verbose, "Initializing workspace service");

                if (Workspace != null)
                {
                    Workspace.WorkspacePath = parameters.RootPath;
                }
                await Task.FromResult(0);
            });

            // Register a shutdown request that disposes the workspace
            serviceHost.RegisterShutdownTask(async (parameters, context) =>
            {
                Logger.Write(TraceEventType.Verbose, "Shutting down workspace service");

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

        /// <summary>
        /// Adds a new task to be called when a text document closes.
        /// </summary>
        /// <param name="task">Delegate to call when the document closes</param>
        public void RegisterTextDocCloseCallback(TextDocCloseCallback task)
        {
            TextDocCloseCallbacks.Add(task);
        }

        /// <summary>
        /// Adds a new task to be called when a file is opened
        /// </summary>
        /// <param name="task">Delegate to call when a document is opened</param>
        public void RegisterTextDocOpenCallback(TextDocOpenCallback task)
        {
            TextDocOpenCallbacks.Add(task);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles text document change events
        /// </summary>
        internal Task HandleDidChangeTextDocumentNotification(
            DidChangeTextDocumentParams textChangeParams,
            EventContext eventContext)
        {
            try
            {
                StringBuilder msg = new StringBuilder();
                msg.Append("HandleDidChangeTextDocumentNotification");
                List<ScriptFile> changedFiles = new List<ScriptFile>();

                // A text change notification can batch multiple change requests
                foreach (var textChange in textChangeParams.ContentChanges)
                {
                    string fileUri = textChangeParams.TextDocument.Uri ?? textChangeParams.TextDocument.Uri; 
                    msg.AppendLine(string.Format("  File: {0}", fileUri));

                    ScriptFile changedFile = Workspace.GetFile(fileUri);
                    if (changedFile != null)
                    {
                        changedFile.ApplyChange(
                            GetFileChangeDetails(
                                textChange.Range.Value,
                                textChange.Text));

                        changedFiles.Add(changedFile);
                    }
                }

                Logger.Write(TraceEventType.Verbose, msg.ToString());

                var handlers = TextDocChangeCallbacks.Select(t => t(changedFiles.ToArray(), eventContext));
                return Task.WhenAll(handlers);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
                // Swallow exceptions here to prevent us from crashing
                // TODO: this probably means the ScriptFile model is in a bad state or out of sync with the actual file; we should recover here
                return Task.FromResult(true);
            }
        }

        internal async Task HandleDidOpenTextDocumentNotification(
            DidOpenTextDocumentNotification openParams,
            EventContext eventContext)
        {
            try
            {
                Logger.Write(TraceEventType.Verbose, "HandleDidOpenTextDocumentNotification");

                if (IsScmEvent(openParams.TextDocument.Uri))
                {
                    return;
                }

                // read the SQL file contents into the ScriptFile 
                ScriptFile openedFile = Workspace.GetFileBuffer(openParams.TextDocument.Uri, openParams.TextDocument.Text);
                if (openedFile == null)
                {
                    return;
                }
                // Propagate the changes to the event handlers
                var textDocOpenTasks = TextDocOpenCallbacks.Select(
                    t => t(openParams.TextDocument.Uri, openedFile, eventContext));

                await Task.WhenAll(textDocOpenTasks);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
                // Swallow exceptions here to prevent us from crashing
                // TODO: this probably means the ScriptFile model is in a bad state or out of sync with the actual file; we should recover here
                return;
            }
        }

        internal async Task HandleDidCloseTextDocumentNotification(
           DidCloseTextDocumentParams closeParams,
           EventContext eventContext)
        {
            try
            {
                Logger.Write(TraceEventType.Verbose, "HandleDidCloseTextDocumentNotification");

                if (IsScmEvent(closeParams.TextDocument.Uri)) 
                {
                    return;
                }

                // Skip closing this file if the file doesn't exist
                var closedFile = Workspace.GetFile(closeParams.TextDocument.Uri);
                if (closedFile == null)
                {
                    return;
                }

                // Trash the existing document from our mapping
                Workspace.CloseFile(closedFile);

                // Send out a notification to other services that have subscribed to this event
                var textDocClosedTasks = TextDocCloseCallbacks.Select(t => t(closeParams.TextDocument.Uri, closedFile, eventContext));
                await Task.WhenAll(textDocClosedTasks);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
                // Swallow exceptions here to prevent us from crashing
                // TODO: this probably means the ScriptFile model is in a bad state or out of sync with the actual file; we should recover here
                return;
            }
        }

        /// <summary>
        /// Handles the configuration change event
        /// </summary>
        internal async Task HandleDidChangeConfigurationNotification(
            DidChangeConfigurationParams<TConfig> configChangeParams,
            EventContext eventContext)
        {
            try
            {
                Logger.Write(TraceEventType.Verbose, "HandleDidChangeConfigurationNotification");

                // Propagate the changes to the event handlers
                var configUpdateTasks = ConfigChangeCallbacks.Select(
                    t => t(configChangeParams.Settings, CurrentSettings, eventContext));
                await Task.WhenAll(configUpdateTasks);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
                // Swallow exceptions here to prevent us from crashing
                // TODO: this probably means the ScriptFile model is in a bad state or out of sync with the actual file; we should recover here
                return;
            }
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
        
        internal static bool IsScmEvent(string filePath)
        {
            // if the URI is prefixed with git: then we want to skip processing that file
            return filePath.StartsWith("git:");
        }

        #endregion
    }
}
