//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class GetSchemaModelRequestParams
    {
        /// <summary>
        /// URI identifying the connection to perform the action on. Generally the connection is picked from an existing OE connection.
        /// </summary>
        public string ConnectionUri { get; set; }
        /// <summary>
        /// Access token for the connection
        /// </summary>
        public string AccessToken { get; set; }
        /// <summary>
        /// Gets or sets the name of the database. Database name is required to get the schema model for the database.
        /// </summary>
        public string DatabaseName { get; set; }
    }

    public class GetSchemaModelResponse
    {
        public SchemaModel SchemaModel { get; set; }
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Request to get the schema model
    /// </summary>
    public class GetSchemaModelRequest
    {
        public static readonly
            RequestType<GetSchemaModelRequestParams, GetSchemaModelResponse> Type =
            RequestType<GetSchemaModelRequestParams, GetSchemaModelResponse>.Create("schemaDesigner/getSchemaModel");
    }
}