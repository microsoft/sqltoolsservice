//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class DropDatabaseRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// The target database name.
        /// </summary>
        public string Database { get; set; }
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
        /// Whether to generate a TSQL script for the operation instead of dropping the database
        /// </summary>
        public bool GenerateScript { get; set; }
    }

    public class DropDatabaseResponse
    {
        /// <summary>
        /// The task id associated with the drop operation when executed.
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// The generated T-SQL script when the request runs in script mode.
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// The task failure message when the drop operation completes unsuccessfully.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    public class DropDatabaseRequest
    {
        public static readonly RequestType<DropDatabaseRequestParams, DropDatabaseResponse> Type = RequestType<DropDatabaseRequestParams, DropDatabaseResponse>.Create("objectManagement/dropDatabase");
    }
}