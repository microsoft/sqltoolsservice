//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;

namespace Microsoft.SqlTools.LanguageService.LanguageServices.Contracts
{
    /// <summary>
    /// Custom <c>sql/listSchemas</c> request — returns the schemas defined in the SQL project that
    /// owns the given document. Used to populate the "Move to Schema" target picker.
    /// </summary>
    public class ListProjectSchemasRequest
    {
        public static readonly
            RequestType<ListProjectSchemasParams, ListProjectSchemasResponse> Type =
            RequestType<ListProjectSchemasParams, ListProjectSchemasResponse>.Create("sql/listSchemas");
    }

    /// <summary>
    /// Parameters sent by the client for a <c>sql/listSchemas</c> request.
    /// </summary>
    public class ListProjectSchemasParams
    {
        /// <summary>The document whose owning project's schemas are requested.</summary>
        public TextDocumentIdentifier TextDocument { get; set; }
    }

    /// <summary>
    /// Response returned by the server for a <c>sql/listSchemas</c> request.
    /// </summary>
    public class ListProjectSchemasResponse
    {
        /// <summary>The distinct schema names defined in the project, sorted case-insensitively.</summary>
        public string[] Schemas { get; set; }
    }
}
