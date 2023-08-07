//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class DeleteDatabaseRequestParams : GeneralRequestDetails
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
        /// Whether to delete the backup and restore history for this database
        /// </summary>
        public bool DeleteBackupHistory { get; set; }
        /// <summary>
        /// Whether to generate a TSQL script for the operation instead of Deleteing the database
        /// </summary>
        public bool GenerateScript { get; set; }
    }

    public class DeleteDatabaseRequest
    {
        public static readonly RequestType<DeleteDatabaseRequestParams, string> Type = RequestType<DeleteDatabaseRequestParams, string>.Create("objectManagement/deleteDatabase");
    }
}