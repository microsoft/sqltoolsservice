//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.Dmp.Contracts.Hosting
{
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
        [Obsolete]
        public string RootPath { get; set; }
        
        /// <summary>
        /// Root path of the editor's open workspace. If null, it is assumed that a file was opened
        /// without having a workspace open.
        /// </summary>
        [Obsolete]
        public string RootUri { get; set; }
    }

    public class InitializeError
    {
        /// <summary>
        /// Gets or sets a boolean indicating whether the client should retry
        /// sending the Initialize request after showing the error to the user.
        /// </summary>
        public bool Retry { get; set;}
    }
    
    public class InitializeResult
    {
        /// <summary>
        /// Capabilities provided by the language server component of this service host
        /// </summary>
        public LanguageServiceCapabilities Capabilities { get; set; }
    }

    public class InitializeRequest
    {
        public static readonly
            RequestType<InitializeParams, InitializeResult> Type =
            RequestType<InitializeParams, InitializeResult>.Create("initialize");
    }
}