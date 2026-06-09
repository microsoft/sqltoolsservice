//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;

namespace Microsoft.SqlTools.LanguageService.LanguageServices.Contracts
{
    /// <summary>
    /// Custom <c>sql/rename</c> request — extends the standard LSP rename with element
    /// metadata so the client can write a <c>.refactorlog</c> entry without a separate round-trip.
    /// </summary>
    public class SqlSymbolRenameRequest
    {
        public static readonly
            RequestType<SqlSymbolRenameParams, SqlSymbolRenameResponse> Type =
            RequestType<SqlSymbolRenameParams, SqlSymbolRenameResponse>.Create("sql/rename");
    }

    /// <summary>
    /// Parameters sent by the client for a <c>sql/rename</c> request.
    /// </summary>
    public class SqlSymbolRenameParams : TextDocumentPosition
    {
        /// <summary>The new name typed by the user in the rename input box.</summary>
        public string NewName { get; set; }
    }

    /// <summary>
    /// Response returned by the server for a <c>sql/rename</c> request.
    /// </summary>
    public class SqlSymbolRenameResponse
    {
        /// <summary>
        /// WorkspaceEdit changes grouped by file URI.
        /// Null when the cursor is not on a renameable symbol.
        /// </summary>
        public Dictionary<string, List<TextEdit>> Changes { get; set; }

        /// <summary>
        /// The original (unbracketed) element name at the cursor position.
        /// Used by the client to write the <c>.refactorlog</c> entry.
        /// </summary>
        public string ElementName { get; set; }

        /// <summary>The new name as echoed back from the request.</summary>
        public string NewName { get; set; }
    }
}
