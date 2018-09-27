//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.DataProtocol.Contracts.Workspace;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.SqlTools.CoreServices.Workspace
{
    /// <summary>
    /// Class for handling requests/events that deal with the state of the workspace, including the
    /// opening and closing of files, the changing of configuration, etc.
    /// </summary>
    /// <typeparam name="TConfig">
    /// The type of the class used for serializing and deserializing the configuration. Must be the
    /// actual type of the instance otherwise deserialization will be incomplete.
    /// </typeparam>
    public class SettingsService<TConfig> where TConfig : class, new()
    {

        #region Singleton Instance Implementation

        private static Lazy<SettingsService<TConfig>> instance = new Lazy<SettingsService<TConfig>>(() => new SettingsService<TConfig>());

        public static SettingsService<TConfig> Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Default, parameterless constructor.
        /// TODO: Figure out how to make this truely singleton even with dependency injection for tests
        /// </summary>
        public SettingsService()
        {
            ConfigChangeCallbacks = new List<ConfigChangeCallback>();
            CurrentSettings = new TConfig();
        }

        #endregion

        #region Properties
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
        /// List of callbacks to call when the configuration of the workspace changes
        /// </summary>
        private List<ConfigChangeCallback> ConfigChangeCallbacks { get; set; }

        #endregion

        #region Public Methods

        public void InitializeService(IServiceHost serviceHost)
        {
            // Register the handlers for when changes to the workspae occur
            serviceHost.SetAsyncEventHandler(DidChangeConfigurationNotification<TConfig>.Type, HandleDidChangeConfigurationNotification);
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

        #endregion

        #region Event Handlers

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
    }
}
