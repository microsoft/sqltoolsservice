//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class PublishSessionRequest
    {
        public string? SessionId { get; set; }
    }

    public class PublishSessionResponse
    {
    }

    public class PublishSession
    {
        /// <summary>
        /// Request to publish the changes in the schema model
        /// </summary>
        public static readonly RequestType<PublishSessionRequest, PublishSessionResponse> Type = RequestType<PublishSessionRequest, PublishSessionResponse>.Create("schemaDesigner/publishSession");
    }


}