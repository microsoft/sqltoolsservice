//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class DetachDatabaseRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// SFC (SMO) URN identifying the object  
        /// </summary>
        public string ObjectUrn { get; set; }
        /// <summary>
        /// URI of the underlying connection for this request
        /// </summary>
        public string ConnectionUri { get; set; }
        /// <summary>
        /// Whether to drop active connections to this database
        /// </summary>
        public bool DropConnections { get; set; }
        /// <summary>
        /// Whether to update the optimization statistics related to this database
        /// </summary>
        public bool UpdateStatistics { get; set; }
        /// <summary>
        /// Whether to generate a TSQL script for the operation instead of detaching the database
        /// </summary>
        public bool GenerateScript { get; set; }
    }

    public class DetachDatabaseRequest
    {
        public static readonly RequestType<DetachDatabaseRequestParams, string> Type = RequestType<DetachDatabaseRequestParams, string>.Create("objectManagement/detachDatabase");
    }
}