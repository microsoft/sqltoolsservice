//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    /// <summary>
    /// LSP textDocument/rename request (F2 rename).
    /// Response is a <see cref="WorkspaceEdit"/> that VS Code applies across all affected files.
    /// </summary>
    public class RenameRequest
    {
        public static readonly
            RequestType<RenameParams, WorkspaceEdit> Type =
            RequestType<RenameParams, WorkspaceEdit>.Create("textDocument/rename");
    }

    /// <summary>
    /// Parameters sent by the client for a textDocument/rename request.
    /// </summary>
    public class RenameParams : TextDocumentPosition
    {
        /// <summary>The new name the user typed in the rename box.</summary>
        public string? NewName { get; set; }
    }

    /// <summary>
    /// A workspace-wide set of text edits, one list per affected file URI.
    /// Null means the rename is not supported at the cursor position.
    /// </summary>
    public class WorkspaceEdit
    {
        /// <summary>
        /// Map from file URI → list of <see cref="TextEdit"/> to apply in that file.
        /// </summary>
        public Dictionary<string, List<TextEdit>>? Changes { get; set; }
    }
}
