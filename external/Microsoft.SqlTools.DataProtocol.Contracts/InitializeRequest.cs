//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.DataProtocol.Contracts.Common;
using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.DataProtocol.Contracts
{
    public enum TraceOptions
    {
        Off,
        Messages,
        Verbose
    }
    
    /// <summary>
    /// Parameters provided with an initialize request
    /// </summary>
    /// <remarks>
    /// This is the initialize request parameters used by VSCode language server. These are
    /// provided here for convenience of implementing initialization request handlers when the
    /// server is expected to be used by the VSCode language client. You are not obligated to use
    /// these initialization params when writing your server, nor are you even obligated to
    /// implement an initialize handler. However, the VSCode language client will always send an
    /// initialize request when starting the server and will not consider the server "ready" until
    /// a response to the init request has been received.
    /// </remarks>
    public class InitializeParameters
    {
        /// <summary>
        /// Capabilities of the initializing client
        /// </summary>
        public ClientCapabilities.ClientCapabilities Capabilities { get; set; }
        
        /// <summary>
        /// Root path of the workspace, is null if no folder is open.
        /// </summary>
        /// <remarks>
        /// This function has been deprecated in favor of workspace folders
        /// </remarks>
        [Obsolete("Deprecated in favor of rootUri")]
        public string RootPath { get; set; }
        
        /// <summary>
        /// Root URI of the workspace, is null if no folder is open. If both <see cref="RootPath"/>
        /// and <see cref="RootUri"/> are set, <see cref="RootUri"/> should be used.
        /// </summary>
        /// <remarks>
        /// This function has been deprecated in favor of workspace folders
        /// </remarks>
        [Obsolete("Deprecated in favor of workspace folders")]
        public string RootUri { get; set; }
        
        /// <summary>
        /// Initial trace setting. If omitted, trace is disabled <see cref="TraceOptions.Off"/>
        /// </summary>
        public TraceOptions? Trace { get; set; }
        
        /// <summary>
        /// Workspace folders that are open at the time of initialization. If this is provided, it
        /// takes precedence over <see cref="RootUri"/> and <see cref="RootPath"/>
        /// </summary>
        public WorkspaceFolder[] WorkspaceFolders { get; set; }
    }

    /// <summary>
    /// Parameters provided as a result to an initialize request
    /// </summary>
    /// <remarks>
    /// This is the initialize result parameters used by VSCode language server. These are provided
    /// for convenience of implementing initialization request handlers when the server is expected
    /// to be used by the VSCode language client. You are not obligated to use these initialization
    /// params when writing your server, nor are you even obligated to implement an initialize
    /// handler. However, the VSCode language client will always send an initialize request when
    /// starting the server and will not consider the server "ready" until a response to the init
    /// request has been received.
    /// </remarks>
    public class InitializeResponse
    {
        /// <summary>
        /// Capabilities that this server provides
        /// </summary>
        public ServerCapabilities.ServerCapabilities Capabilities { get; set; }
    }

    public class InitializeRequest
    {
        public static readonly RequestType<InitializeParameters, InitializeResponse> Type =
            RequestType<InitializeParameters, InitializeResponse>.Create("initialize");
    }
}