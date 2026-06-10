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
    /// The kind of an inlay hint.
    /// </summary>
    public enum InlayHintKind
    {
        /// <summary>
        /// An inlay hint that is for a type annotation.
        /// </summary>
        Type = 1,

        /// <summary>
        /// An inlay hint that is for a parameter.
        /// </summary>
        Parameter = 2,
    }

    /// <summary>
    /// Parameters for an <see cref="InlayHintRequest"/>. Only hints that fall
    /// within <see cref="Range"/> need to be returned.
    /// </summary>
    public class InlayHintParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Gets or sets the visible document range hints are requested for.
        /// </summary>
        public Range Range { get; set; }
    }

    /// <summary>
    /// An inlay hint rendered inline in the editor at a specific position.
    /// </summary>
    public class InlayHint
    {
        /// <summary>
        /// Gets or sets the position (zero-based) at which the hint is shown.
        /// </summary>
        public Position Position { get; set; }

        /// <summary>
        /// Gets or sets the hint label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the kind of the hint.
        /// </summary>
        public InlayHintKind? Kind { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether padding is rendered before the hint.
        /// </summary>
        public bool? PaddingLeft { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether padding is rendered after the hint.
        /// </summary>
        public bool? PaddingRight { get; set; }

        /// <summary>
        /// Gets or sets an optional tooltip shown when hovering the hint.
        /// </summary>
        public string Tooltip { get; set; }
    }

    /// <summary>
    /// Computes the inlay hints for a range of a text document.
    /// </summary>
    public class InlayHintRequest
    {
        public static readonly
            RequestType<InlayHintParams, InlayHint[]> Type =
            RequestType<InlayHintParams, InlayHint[]>.Create("textDocument/inlayHint");
    }
}
