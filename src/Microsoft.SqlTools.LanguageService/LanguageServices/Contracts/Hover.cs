//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;

namespace Microsoft.SqlTools.LanguageService.LanguageServices.Contracts
{
    /// <summary>
    /// A markup content literal represents a string value whose content is interpreted based on its kind flag.
    /// </summary>
    /// <seealso href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#markupContent"/>
    public class MarkupContent
    {
        /// <summary>
        /// The type of the markup.
        /// </summary>
        /// <remarks>Currently, the protocol supports <c>plaintext</c> and <c>markdown</c> as markup kinds.</remarks>
        public string Kind { get; set; }

        /// <summary>
        /// The content itself.
        /// </summary>
        public string Value { get; set; }
    }

    public class Hover
    {
        public MarkupContent Contents { get; set; }

        public Range? Range { get; set; }
    }

    public class HoverRequest
    {
        public static readonly
            RequestType<TextDocumentPosition, Hover> Type =
            RequestType<TextDocumentPosition, Hover>.Create("textDocument/hover");

    }
}

