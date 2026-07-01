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

        /// <summary>
        /// Current content of the project's <c>.refactorlog</c> file, or <see langword="null"/>/empty
        /// if the project does not have one yet. The server appends the new rename operation to this
        /// content and returns the full document in <see cref="SqlSymbolRenameResponse.RefactorLogContent"/>.
        /// </summary>
        public string ExistingRefactorLogContent { get; set; }
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
        /// Full content of the <c>.refactorlog</c> file with the new rename operation appended,
        /// ready for the client to write. Null when the renamed symbol does not require a
        /// <c>.refactorlog</c> entry (e.g. the element type could not be determined).
        /// </summary>
        public string RefactorLogContent { get; set; }

        /// <summary>
        /// The new name as echoed back from the request.
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// When non-null, a message to surface to the user. Check <see cref="IsWarning"/> to
        /// determine whether to show a confirmation dialog (<see langword="true"/>) or a blocking
        /// error (<see langword="false"/>).
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// <see langword="true"/> when <see cref="Message"/> is a confirmation warning that the user
        /// can dismiss to proceed; <see langword="false"/> (default) when it is a hard rejection.
        /// </summary>
        public bool IsWarning { get; set; }
    }
}
