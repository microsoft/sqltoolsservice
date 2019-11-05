//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Formatter.Contracts
{
    /// <summary>
    /// A formatting request to process an entire document
    /// </summary>
    public class DocumentFormattingRequest
    {
        public static readonly
            RequestType<DocumentFormattingParams, TextEdit[]> Type =
            RequestType<DocumentFormattingParams, TextEdit[]>.Create("textDocument/formatting");
    }

    /// <summary>
    /// A formatting request to process a specific range inside a document
    /// </summary>
    public class DocumentRangeFormattingRequest
    {
        public static readonly
            RequestType<DocumentRangeFormattingParams, TextEdit[]> Type =
            RequestType<DocumentRangeFormattingParams, TextEdit[]>.Create("textDocument/rangeFormatting");
    }

    /// <summary>
    /// A formatting request to handle a user typing, giving a chance to update the text based on this
    /// </summary>
    public class DocumentOnTypeFormattingRequest
    {
        public static readonly
            RequestType<DocumentOnTypeFormattingParams, TextEdit[]> Type =
            RequestType<DocumentOnTypeFormattingParams, TextEdit[]>.Create("textDocument/onTypeFormatting");
    }


    /// <summary>
    /// Params for the <see cref="DocumentFormattingRequest"/>
    /// </summary>
    public class DocumentFormattingParams
    {

        /// <summary>
        /// The document to format.
        /// </summary>
        public TextDocumentIdentifier TextDocument { get; set; }
        
        /// <summary>
        /// The formatting options
        /// </summary>
        public FormattingOptions Options { get; set; }

    }


    /// <summary>
    /// Params for the <see cref="DocumentRangeFormattingRequest"/>
    /// </summary>
    public class DocumentRangeFormattingParams : DocumentFormattingParams
    {
        
        /// <summary>
        /// The range to format
        /// </summary>
        public Range Range { get; set; }
        
    }

    /// <summary>
    /// Params for the <see cref="DocumentOnTypeFormattingRequest"/>
    /// </summary>
    public class DocumentOnTypeFormattingParams : DocumentFormattingParams
    {
        /// <summary>
        /// The position at which this request was sent.

        /// </summary>
        Position Position { get; set; }

        /// <summary>
        /// The character that has been typed.

        /// </summary>
        string Ch { get; set; }
    }

    /// <summary>
    /// Value-object describing what options formatting should use.
    /// </summary>
    public class FormattingOptions
    {
        /// <summary>
        /// Size of a tab in spaces
        /// </summary>
        public int TabSize { get; set; }

        /// <summary>
        /// Prefer spaces over tabs.
        /// </summary>
        public bool InsertSpaces { get; set; }

        // TODO there may be other options passed by VSCode - format is 
        // [key: string]: boolean | number | string;
        // Determine how these might be passed and add them here
}

}
