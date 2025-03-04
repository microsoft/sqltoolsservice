//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class DisposeSchemaDesignerSessionParams
    {
        /// <summary>
        /// Unique id for the session to dispose
        /// </summary>
        public string SessionId { get; set; }
    }

    public class DisposeSchemaDesignerSessionResponse
    {
    }

    /// <summary>
    /// Request to dispose the schema designer session
    /// </summary>
    public class DisposeSchemaDesignerSessionRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DisposeSchemaDesignerSessionParams, DisposeSchemaDesignerSessionResponse> Type =
            RequestType<DisposeSchemaDesignerSessionParams, DisposeSchemaDesignerSessionResponse>.Create("schemaDesigner/disposeSchemaDesignerSession");
    }
}