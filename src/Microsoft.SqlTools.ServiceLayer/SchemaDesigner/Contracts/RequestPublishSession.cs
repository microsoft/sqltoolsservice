//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class PublishSessionRequest
    {
        /// <summary>
        /// Unique id for the session to publish
        /// </summary>
        public string SessionId { get; set; }
    }

    public class PublishSessionResponse
    {
        /// <summary>
        /// The result of the publish operation
        /// </summary>
        public string Result { get; set; }
    }

    /// <summary>
    /// Request to publish the schema designer session
    /// </summary>
    public class PublishSession
    {
        public static readonly RequestType<PublishSessionRequest, PublishSessionResponse> Type =
            RequestType<PublishSessionRequest, PublishSessionResponse>.Create("schemaDesigner/publishSession");
    }

}