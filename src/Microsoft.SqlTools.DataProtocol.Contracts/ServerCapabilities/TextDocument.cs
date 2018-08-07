//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.DataProtocol.Contracts.Utilities;

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{
    public enum TextDocumentSyncKind
    {
        None = 0,
        Full = 1,
        Incremental = 2
    }

    /// <summary>
    /// Defines options for sae notifications
    /// </summary>
    public class TextDocumentSaveOptions
    {
        /// <summary>
        /// Whether the client should send the content of the file being saved with the save notification
        /// </summary>
        public bool? IncludeText { get; set; }
    }

    public class TextDocumentSync : Union<TextDocumentSyncOptions, TextDocumentSyncKind?>
    {
        public static TextDocumentSync FromTextDocumentSyncKind(TextDocumentSyncKind value)
        {
            return new TextDocumentSync
            {
                value1 = null,
                value2 = value
            };
        }

        public static TextDocumentSync FromTextDocumentSyncOptions(TextDocumentSyncOptions value)
        {
            return new TextDocumentSync
            {
                value1 = value,
                value2 = null
            };
        }
    }
    
    public class TextDocumentSyncOptions
    {
        /// <summary>
        /// What kind of change notifications are sent to the server
        /// </summary>
        public TextDocumentSyncKind? Change { get; set; }
        
        /// <summary>
        /// Whether open and close notifications are sent to the server
        /// </summary>
        public bool? OpenClose { get; set; }
        
        /// <summary>
        /// Options for save notifications
        /// </summary>
        public TextDocumentSaveOptions SaveOptions { get; set; }
    
        /// <summary>
        /// Whether notifications indicating the client will save are sent to the server
        /// </summary>
        public bool? WillSave { get; set; }
        
        /// <summary>
        /// Whether requests should be sent to the server when the client is about to save
        /// </summary>
        public bool? WillSaveWaitUntil { get; set; }
    }
}
