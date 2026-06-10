//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    /// <summary>
    /// Declares the semantic token types and modifiers the language service can
    /// emit. The ordering of these arrays defines the numeric indices used in
    /// the encoded <see cref="SemanticTokens.Data"/> payload, so the same legend
    /// must be advertised to the client through the server capabilities.
    /// </summary>
    public static class SemanticTokensLegend
    {
        /// <summary>
        /// Semantic token types. The index of each entry is the value emitted in
        /// the encoded token data.
        /// </summary>
        public static readonly string[] TokenTypes =
        {
            "keyword",   // 0
            "comment",   // 1
            "number",    // 2
            "string",    // 3
            "operator",  // 4
            "variable",  // 5
            "function",  // 6
            "class",     // 7
            "macro",     // 8
        };

        /// <summary>
        /// Semantic token modifiers. None are emitted today but the (empty) set
        /// is still advertised so the client legend stays in sync.
        /// </summary>
        public static readonly string[] TokenModifiers = System.Array.Empty<string>();

        public const int Keyword = 0;
        public const int Comment = 1;
        public const int Number = 2;
        public const int String = 3;
        public const int Operator = 4;
        public const int Variable = 5;
        public const int Function = 6;
        public const int Class = 7;
        public const int Macro = 8;
    }

    /// <summary>
    /// Parameters for a <see cref="SemanticTokensRequest"/>.
    /// </summary>
    public class SemanticTokensParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }
    }

    /// <summary>
    /// The result of a full document semantic tokens request. <see cref="Data"/>
    /// is the LSP delta-encoded integer array (five integers per token:
    /// deltaLine, deltaStartChar, length, tokenType, tokenModifiers).
    /// </summary>
    public class SemanticTokens
    {
        /// <summary>
        /// Gets or sets an optional result id used for delta requests. The
        /// language service does not currently support deltas.
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// Gets or sets the delta-encoded semantic token data.
        /// </summary>
        public int[] Data { get; set; }
    }

    /// <summary>
    /// Computes the semantic tokens for a full text document.
    /// </summary>
    public class SemanticTokensRequest
    {
        public static readonly
            RequestType<SemanticTokensParams, SemanticTokens> Type =
            RequestType<SemanticTokensParams, SemanticTokens>.Create("textDocument/semanticTokens/full");
    }
}
