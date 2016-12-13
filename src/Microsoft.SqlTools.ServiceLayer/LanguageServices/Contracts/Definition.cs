//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    public class DefinitionRequest
    {
        public static readonly
            RequestType<TextDocumentPosition, Location[]> Type =
            RequestType<TextDocumentPosition, Location[]>.Create("textDocument/definition");
    }

    /// <summary>
    /// Error object for Definition
    /// </summary>
    public class DefinitionError
    {
        /// <summary>
        /// Error message 
        /// </summary>
        public string message { get; set; }
    }

    /// <summary>
    /// Result object for Definition
    /// </summary>
    public class DefinitionResult
    {
        /// <summary>
        /// True, if definition error occured
        /// </summary>
        public bool IsErrorResult;
        /// <summary>
        /// Error message, if any
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// location object representing the definition script file
        /// </summary>
        public Location[] Locations;
        
    }
}

