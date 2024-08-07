//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    public enum DocumentHighlightKind 
    {
        Text = 1,
        Read = 2,
        Write = 3
    }

    public class DocumentHighlight 
    {
	    public Range Range { get; set; }

        public DocumentHighlightKind Kind { get; set; }
    }

    public class DocumentHighlightRequest
    {
        public static readonly
            RequestType<TextDocumentPosition, DocumentHighlight[]> Type =
            RequestType<TextDocumentPosition, DocumentHighlight[]>.Create("textDocument/documentHighlight");
    }
}

