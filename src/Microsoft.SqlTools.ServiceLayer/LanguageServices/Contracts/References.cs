//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.WorkspaceServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    public class ReferencesRequest
    {
        public static readonly
            RequestType<ReferencesParams, Location[]> Type =
            RequestType<ReferencesParams, Location[]>.Create("textDocument/references");
    }

    public class ReferencesParams : TextDocumentPosition
    {
        public ReferencesContext Context { get; set; }
    }

    public class ReferencesContext
    {
        public bool IncludeDeclaration { get; set; }
    }
}

