//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class RenameDatabaseRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// The source database name.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// The new database name.
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// URI of the underlying connection for this request.
        /// </summary>
        public string ConnectionUri { get; set; }

        /// <summary>
        /// Whether to drop active connections to this database before renaming.
        /// </summary>
        public bool DropConnections { get; set; }

        /// <summary>
        /// Whether to generate a T-SQL script for the operation instead of renaming the database.
        /// </summary>
        public bool GenerateScript { get; set; }
    }

    public class RenameDatabaseResponse
    {
        /// <summary>
        /// The task id associated with the rename operation when executed.
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// The generated T-SQL script when the request runs in script mode.
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// The task failure message when the rename operation completes unsuccessfully.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    public class RenameDatabaseRequest
    {
        public static readonly RequestType<RenameDatabaseRequestParams, RenameDatabaseResponse> Type = RequestType<RenameDatabaseRequestParams, RenameDatabaseResponse>.Create("objectManagement/renameDatabase");
    }
}
