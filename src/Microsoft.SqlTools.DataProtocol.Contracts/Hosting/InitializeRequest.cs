//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Hosting
{
    /// <summary>
    /// Parameters sent by the tools service client to the tools service server to initialize it
    /// </summary>
    public class InitializeParams
    {
        /// <summary>
        /// Capabilities of the client that is initializing this service host
        /// </summary>
        public ClientCapabilities Capabilities { get; set; }
        
        /// <summary>
        /// ID of the process that is initializing this service host
        /// </summary>
        public int ProcessId { get; set; }
        
        /// <summary>
        /// Root path of the editor's open workspace. If null, it is assumed that a file was opened
        /// without having a workspace open.
        /// </summary>
        /// <remarks>If both RootPath and RootUri are available, RootUri is preferred</remarks>
        [Obsolete("RootPath is deprecated in favor RootUri")]
        public string RootPath { get; set; }
        
        /// <summary>
        /// Root path of the editor's open workspace. If null, it is assumed that a file was opened
        /// without having a workspace open.
        /// </summary>
        public string RootUri { get; set; }
    }

    /// <summary>
    /// Data type of the response error if the initialize request fails
    /// </summary>
    public class InitializeError
    {
        /// <summary>
        /// Gets or sets a boolean indicating whether the client should retry
        /// sending the Initialize request after showing the error to the user.
        /// </summary>
        public bool Retry { get; set;}
    }
    
    /// <summary>
    /// The result returned from an initialize request
    /// </summary>
    public class InitializeResult
    {
        /// <summary>
        /// Capabilities provided by the language server component of this service host
        /// </summary>
        public LanguageServiceCapabilities Capabilities { get; set; }
    }

    /// <summary>
    /// The initialize request is sent from the client to the server. It is sent once as the 
    /// request after starting up the server. The requests parameter is of type 
    /// <see cref="InitializeParams"/> the response is of type <see cref="InitializeResult"/>.
    /// </summary>
    public class InitializeRequest
    {
        public static readonly
            RequestType<InitializeParams, InitializeResult> Type =
            RequestType<InitializeParams, InitializeResult>.Create("initialize");
    }
}