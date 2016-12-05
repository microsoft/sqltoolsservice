//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    public class DefinitionRequest
    {
        public static readonly
            RequestType<TextDocumentPosition, Location[]> Type =
            RequestType<TextDocumentPosition, Location[]>.Create("textDocument/definition");
    }

    /// <summary>
    /// Parameters sent back with a definition event
    /// </summary>
    public class DefinitionSentParams
    {
    }

    /// <summary>
    /// Event sent when the language service sent the definition
    /// </summary>
    public class DefinitionSentNotification
    {
        public static readonly
            EventType<DefinitionSentParams> Type =
            EventType<DefinitionSentParams>.Create("textDocument/definitionSent");
    }
}

