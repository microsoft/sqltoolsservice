//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class CreateSessionRequest
    {
        /// <summary>
        /// URI identifying the connection to perform the action on. Generally the connection is picked from an existing OE connection.
        /// </summary>
        public string ConnectionUri { get; set; }
        /// <summary>
        /// Gets or sets the name of the database. Database name is required to get the schema model for the database.
        /// </summary>
        public string DatabaseName { get; set; }
    }

    public class CreateSessionResponse
    {
        /// <summary>
        /// Gets or sets the schema model
        /// </summary>
        public SchemaDesignerModel Schema { get; set; }
        /// <summary>
        /// Gets or sets the datatypes available in the database
        /// </summary>
        public List<string> DataTypes { get; set; }
        /// <summary>
        /// Gets or sets the schema names available in the database
        /// </summary>
        public List<string> SchemaNames { get; set; }
        /// <summary>
        /// Gets or sets the session id
        /// </summary>
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Request to get the schema model
    /// </summary>
    public class CreateSession
    {
        public static readonly
            RequestType<CreateSessionRequest, CreateSessionResponse> Type =
            RequestType<CreateSessionRequest, CreateSessionResponse>.Create("schemaDesigner/createSession");
    }
}