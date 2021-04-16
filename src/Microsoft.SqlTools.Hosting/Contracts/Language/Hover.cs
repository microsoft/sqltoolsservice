//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts.Workspace;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.Contracts.Language
{
    public class MarkedString
    {
        public string Language { get; set; }

        public string Value { get; set; }
    }

    public class Hover
    {
        public MarkedString[] Contents { get; set; }

        public Range? Range { get; set; }
    }

    public class HoverRequest
    {
        public static readonly
            RequestType<TextDocumentPosition, Hover> Type =
            RequestType<TextDocumentPosition, Hover>.Create("textDocument/hover");

    }
}

