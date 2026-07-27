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
    /// Custom <c>sql/moveToSchema</c> request — moves a schema-owned object to a different schema.
    /// Mirrors <see cref="SqlSymbolRenameRequest"/>: it returns the reference edits plus the full
    /// <c>.refactorlog</c> content so the client can write a <c>Move Schema</c> entry without a
    /// separate round-trip.
    /// </summary>
    public class SqlMoveToSchemaRequest
    {
        public static readonly
            RequestType<SqlMoveToSchemaParams, SqlMoveToSchemaResponse> Type =
            RequestType<SqlMoveToSchemaParams, SqlMoveToSchemaResponse>.Create("sql/moveToSchema");
    }

    /// <summary>
    /// Parameters sent by the client for a <c>sql/moveToSchema</c> request.
    /// </summary>
    public class SqlMoveToSchemaParams : TextDocumentPosition
    {
        /// <summary>The target schema the object is moved to, as picked by the user.</summary>
        public string TargetSchema { get; set; }

        /// <summary>
        /// Current content of the project's <c>.refactorlog</c> file, or <see langword="null"/>/empty
        /// if the project does not have one yet. The server appends the new move-schema operation to
        /// this content and returns the full document in
        /// <see cref="SqlMoveToSchemaResponse.RefactorLogContent"/>.
        /// </summary>
        public string ExistingRefactorLogContent { get; set; }
    }

    /// <summary>
    /// Response returned by the server for a <c>sql/moveToSchema</c> request.
    /// </summary>
    public class SqlMoveToSchemaResponse
    {
        /// <summary>
        /// WorkspaceEdit changes grouped by file URI. Null when the cursor is not on a movable object.
        /// </summary>
        public Dictionary<string, List<TextEdit>> Changes { get; set; }

        /// <summary>
        /// Full content of the <c>.refactorlog</c> file with the new move-schema operation appended,
        /// ready for the client to write. Null when the object does not require a <c>.refactorlog</c>
        /// entry (e.g. the element type could not be determined).
        /// </summary>
        public string RefactorLogContent { get; set; }

        /// <summary>The target schema as echoed back from the request.</summary>
        public string TargetSchema { get; set; }

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
