//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.EditorServices.Session;
using Microsoft.SqlTools.LanguageSupport;

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
        /// Gets or sets the Language Service
        /// </summary>
        /// <returns></returns>
        public LanguageService LanguageService { get; set; }

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
            this.LanguageService = new LanguageService(this.SqlToolsContext);

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
       
    }  
}
