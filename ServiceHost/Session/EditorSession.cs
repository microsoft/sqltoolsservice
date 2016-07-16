//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.EditorServices.Session;

namespace Microsoft.SqlTools.EditorServices
{
    /// <summary>
    /// Manages a single session for all editor services.  This 
    /// includes managing all open script files for the session.
    /// </summary>
    public class EditorSession : IDisposable
    {
        #region Properties

        /// <summary>
        /// Gets the Workspace instance for this session.
        /// </summary>
        public Workspace Workspace { get; private set; }

        /// <summary>
        /// Gets the SqlToolsContext instance for this session.
        /// </summary>
        public SqlToolsContext SqlToolsContext { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the session using the provided IConsoleHost implementation
        /// for the ConsoleService.
        /// </summary>
        /// <param name="hostDetails">
        /// Provides details about the host application.
        /// </param>
        /// <param name="profilePaths">
        /// An object containing the profile paths for the session.
        /// </param>
        public void StartSession(HostDetails hostDetails, ProfilePaths profilePaths)
        {
            // Initialize all services
            this.SqlToolsContext = new SqlToolsContext(hostDetails, profilePaths);


            // this.LanguageService = new LanguageService(this.SqlToolsContext);
            // this.DebugService = new DebugService(this.SqlToolsContext);
            // this.ConsoleService = new ConsoleService(this.SqlToolsContext);
            // this.ExtensionService = new ExtensionService(this.SqlToolsContext);

            // this.InstantiateAnalysisService();

            // Create a workspace to contain open files
            this.Workspace = new Workspace(this.SqlToolsContext.SqlToolsVersion);
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


#if false        
        #region Properties

        

        

        /// <summary>
        /// Gets the LanguageService instance for this session.
        /// </summary>
        public LanguageService LanguageService { get; private set; }

        /// <summary>
        /// Gets the AnalysisService instance for this session.
        /// </summary>
        public AnalysisService AnalysisService { get; private set; }

        /// <summary>
        /// Gets the DebugService instance for this session.
        /// </summary>
        public DebugService DebugService { get; private set; }

        /// <summary>
        /// Gets the ConsoleService instance for this session.
        /// </summary>
        public ConsoleService ConsoleService { get; private set; }

        /// <summary>
        /// Gets the ExtensionService instance for this session.
        /// </summary>
        public ExtensionService ExtensionService { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the session using the provided IConsoleHost implementation
        /// for the ConsoleService.
        /// </summary>
        /// <param name="hostDetails">
        /// Provides details about the host application.
        /// </param>
        /// <param name="profilePaths">
        /// An object containing the profile paths for the session.
        /// </param>
        public void StartSession(HostDetails hostDetails, ProfilePaths profilePaths)
        {
            // Initialize all services
            this.SqlToolsContext = new SqlToolsContext(hostDetails, profilePaths);
            this.LanguageService = new LanguageService(this.SqlToolsContext);
            this.DebugService = new DebugService(this.SqlToolsContext);
            this.ConsoleService = new ConsoleService(this.SqlToolsContext);
            this.ExtensionService = new ExtensionService(this.SqlToolsContext);

            this.InstantiateAnalysisService();

            // Create a workspace to contain open files
            this.Workspace = new Workspace(this.SqlToolsContext.SqlToolsVersion);
        }

        /// <summary>
        /// Restarts the AnalysisService so it can be configured with a new settings file.
        /// </summary>
        /// <param name="settingsPath">Path to the settings file.</param>
        public void RestartAnalysisService(string settingsPath)
        {
            this.AnalysisService?.Dispose();
            InstantiateAnalysisService(settingsPath);
        }

        internal void InstantiateAnalysisService(string settingsPath = null)
        {
            // Only enable the AnalysisService if the machine has SqlTools
            // v5 installed.  Script Analyzer works on earlier SqlTools
            // versions but our hard dependency on their binaries complicates
            // the deployment and assembly loading since we would have to
            // conditionally load the binaries for v3/v4 support.  This problem
            // will be solved in the future by using Script Analyzer as a
            // module rather than an assembly dependency.
            if (this.SqlToolsContext.SqlToolsVersion.Major >= 5)
            {
                // AnalysisService will throw FileNotFoundException if
                // Script Analyzer binaries are not included.
                try
                {
                    this.AnalysisService = new AnalysisService(this.SqlToolsContext.ConsoleHost, settingsPath);
                }
                catch (FileNotFoundException)
                {
                    Logger.Write(
                        LogLevel.Warning,
                        "Script Analyzer binaries not found, AnalysisService will be disabled.");
                }
            }
            else
            {
                Logger.Write(
                    LogLevel.Normal,
                    "Script Analyzer cannot be loaded due to unsupported SqlTools version " +
                    this.SqlToolsContext.SqlToolsVersion.ToString());
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of any Runspaces that were created for the
        /// services used in this session.
        /// </summary>
        public void Dispose()
        {
            if (this.AnalysisService != null)
            {
                this.AnalysisService.Dispose();
                this.AnalysisService = null;
            }

            if (this.SqlToolsContext != null)
            {
                this.SqlToolsContext.Dispose();
                this.SqlToolsContext = null;
            }
        }

        #endregion
#endif          
    }  
}
