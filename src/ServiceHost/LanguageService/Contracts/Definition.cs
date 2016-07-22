//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.ServiceHost.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.WorkspaceService.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageService.Contracts
{
    public class DefinitionRequest
    {
        public static readonly
            RequestType<TextDocumentPosition, Location[]> Type =
            RequestType<TextDocumentPosition, Location[]>.Create("textDocument/definition");
    }
}

