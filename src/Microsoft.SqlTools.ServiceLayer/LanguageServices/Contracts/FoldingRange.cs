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
    /// Well-known folding range kinds defined by the language server protocol.
    /// </summary>
    public static class FoldingRangeKind
    {
        public const string Comment = "comment";
        public const string Imports = "imports";
        public const string Region = "region";
    }

    /// <summary>
    /// Represents a folding range that the editor can collapse. Line and
    /// character numbers are zero-based.
    /// </summary>
    public class FoldingRange
    {
        /// <summary>
        /// Gets or sets the zero-based line where the folded range starts.
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Gets or sets the zero-based character offset where the folded range
        /// starts. When omitted the range folds from the end of the start line.
        /// </summary>
        public int? StartCharacter { get; set; }

        /// <summary>
        /// Gets or sets the zero-based line where the folded range ends.
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Gets or sets the zero-based character offset where the folded range ends.
        /// </summary>
        public int? EndCharacter { get; set; }

        /// <summary>
        /// Gets or sets the folding range kind (see <see cref="FoldingRangeKind"/>).
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Gets or sets the text the editor shows for the collapsed range.
        /// </summary>
        public string CollapsedText { get; set; }
    }

    /// <summary>
    /// Parameters for a <see cref="FoldingRangeRequest"/>.
    /// </summary>
    public class FoldingRangeParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }
    }

    /// <summary>
    /// Computes the folding ranges for a text document.
    /// </summary>
    public class FoldingRangeRequest
    {
        public static readonly
            RequestType<FoldingRangeParams, FoldingRange[]> Type =
            RequestType<FoldingRangeParams, FoldingRange[]>.Create("textDocument/foldingRange");
    }
}
