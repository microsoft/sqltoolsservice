//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;

namespace Microsoft.SqlTools.LanguageService.Workspace
{
    /// <summary>
    /// Callback invoked when the workspace configuration changes. Uses the non-generic
    /// <see cref="ILanguageServiceSettings"/> abstraction so consumers don't depend on the concrete
    /// settings type.
    /// </summary>
    public delegate Task WorkspaceConfigChangeCallback(ILanguageServiceSettings newSettings, ILanguageServiceSettings oldSettings, EventContext eventContext);

    /// <summary>Callback invoked when one or more text documents change.</summary>
    public delegate Task WorkspaceTextDocChangeCallback(ScriptFile[] changedFiles, EventContext eventContext);

    /// <summary>Callback invoked when a text document is opened.</summary>
    public delegate Task WorkspaceTextDocOpenCallback(string uri, ScriptFile openFile, EventContext eventContext);

    /// <summary>Callback invoked when a text document is closed.</summary>
    public delegate Task WorkspaceTextDocCloseCallback(string uri, ScriptFile closedFile, EventContext eventContext);

    /// <summary>Callback invoked when a text document is saved.</summary>
    public delegate Task WorkspaceTextDocSaveCallback(string uri, EventContext eventContext);

    /// <summary>
    /// Non-generic abstraction over <see cref="WorkspaceService{TConfig}"/> exposing only the surface
    /// the language service needs, so it does not depend on the concrete settings type (SqlToolsSettings).
    /// </summary>
    public interface ILanguageWorkspaceService
    {
        /// <summary>Gets the workspace that tracks open files.</summary>
        Workspace Workspace { get; }

        /// <summary>Gets the current workspace settings.</summary>
        ILanguageServiceSettings CurrentSettings { get; }

        /// <summary>Registers a callback invoked when the configuration changes.</summary>
        void RegisterConfigChangeCallback(WorkspaceConfigChangeCallback task);

        /// <summary>Registers a callback invoked when text documents change.</summary>
        void RegisterTextDocChangeCallback(WorkspaceTextDocChangeCallback task);

        /// <summary>Registers a callback invoked when a text document is opened.</summary>
        void RegisterTextDocOpenCallback(WorkspaceTextDocOpenCallback task);

        /// <summary>Registers a callback invoked when a text document is closed.</summary>
        void RegisterTextDocCloseCallback(WorkspaceTextDocCloseCallback task);

        /// <summary>Registers a callback invoked when a text document is saved.</summary>
        void RegisterTextDocSaveCallback(WorkspaceTextDocSaveCallback task);
    }
}
