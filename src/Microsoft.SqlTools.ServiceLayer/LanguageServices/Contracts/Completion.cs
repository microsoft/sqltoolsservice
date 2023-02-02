//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Diagnostics;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    public class CompletionRequest
    {
        public static readonly
            RequestType<TextDocumentPosition, CompletionItem[]> Type =
            RequestType<TextDocumentPosition, CompletionItem[]>.Create("textDocument/completion");
    }

    public class CompletionResolveRequest
    {
        public static readonly
            RequestType<CompletionItem, CompletionItem> Type =
            RequestType<CompletionItem, CompletionItem>.Create("completionItem/resolve");
    }

    public class CompletionExtLoadRequest
    {
        public static readonly
            RequestType<CompletionExtensionParams, bool> Type =
            RequestType<CompletionExtensionParams, bool>.Create("completion/extLoad");
    }

    public enum CompletionItemKind
    {
        	Text = 1,
            Method = 2,
            Function = 3,
            Constructor = 4,
            Field = 5,
            Variable = 6,
            Class = 7,
            Interface = 8,
            Module = 9,
            Property = 10,
            Unit = 11,
            Value = 12,
            Enum = 13,
            Keyword = 14,
            Snippet = 15,
            Color = 16,
            File = 17,
            Reference = 18
    }

    public class Command
    {
        /// <summary>
        /// Title of the command.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The identifier of the actual command handler, like `vsintellicode.completionItemSelected`.
        /// </summary>
        public string command { get; set; }

        /// <summary>
        /// A tooltip for the command, when represented in the UI.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// Arguments that the command handler should be invoked with.
        /// </summary>
        public object[] Arguments { get; set; }
    }

    [DebuggerDisplay("Kind = {Kind.ToString()}, Label = {Label}, Detail = {Detail}")]
    public class CompletionItem
    {
        public string Label { get; set; }

        public CompletionItemKind? Kind { get; set; }

        public string Detail { get; set; }

        /// <summary>
        /// Gets or sets the documentation string for the completion item.
        /// </summary>
        public string Documentation { get; set; }

        public string SortText { get; set; }

        public string FilterText { get; set; }

        public string InsertText { get; set; }

        public TextEdit TextEdit { get; set; }

        /// <summary>
        /// Gets or sets a custom data field that allows the server to mark
        /// each completion item with an identifier that will help correlate
        /// the item to the previous completion request during a completion
        /// resolve request.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Exposing a command field for a completion item for passing telemetry
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        /// Whether this completion item is preselected or not
        /// </summary>
        public bool? Preselect { get; set; }
    }
}
