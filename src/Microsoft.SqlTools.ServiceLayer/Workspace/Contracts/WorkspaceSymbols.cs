//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Workspace.Contracts
{
    public class SymbolInformation 
    {
        public string Name { get; set; }

        public SymbolKind Kind { get; set; }

        public Location Location { get; set; }

        public string ContainerName { get; set;}
    }

    public class DocumentSymbolRequest
    {
        public static readonly
            RequestType<DocumentSymbolParams, SymbolInformation[]> Type =
            RequestType<DocumentSymbolParams, SymbolInformation[]>.Create("textDocument/documentSymbol");
    }

    /// <summary>
    /// Defines a set of parameters to send document symbol request
    /// </summary>
    public class DocumentSymbolParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }
    }

    public class WorkspaceSymbolRequest
    {
        public static readonly
            RequestType<WorkspaceSymbolParams, SymbolInformation[]> Type =
            RequestType<WorkspaceSymbolParams, SymbolInformation[]>.Create("workspace/symbol");
    }

    public class WorkspaceSymbolParams
    {
        public string Query { get; set;}
    }
}

