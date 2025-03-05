//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    /// <summary>
    /// Request to get the publish script for the schema designer session
    /// </summary>
    public class GetPublishScriptRequest
    {
        /// <summary>
        /// Unique id for the session to publish
        /// </summary>
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Response containing the publish script for the schema designer session
    /// </summary>
    public class GetPublishScriptResponse
    {
        /// <summary>
        /// The publish script
        /// </summary>
        public SchemaDesignerScriptObject Scripts { get; set; }
    }

    /// <summary>
    /// Request to get the publish script for the schema designer session
    /// </summary>
    public class GetPublishScript
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly RequestType<GetPublishScriptRequest, GetPublishScriptResponse> Type =
            RequestType<GetPublishScriptRequest, GetPublishScriptResponse>.Create("schemaDesigner/getPublishScript");
    }
}