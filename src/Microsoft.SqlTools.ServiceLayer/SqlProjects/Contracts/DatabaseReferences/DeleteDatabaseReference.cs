//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for deleting a database reference
    /// </summary>
    public class DeleteDatabaseReferenceParams : SqlProjectParams
    {
        /// <summary>
        /// Name of the reference to be deleted.  Name of the System DB, path of the sqlproj, or path of the dacpac
        /// </summary>
        public string Name { get; set; }
    }

    public class DeleteDatabaseReferenceRequest
    {
        public static readonly RequestType<SqlProjectScriptParams, ResultStatus> Type = RequestType<SqlProjectScriptParams, ResultStatus>.Create("sqlprojects/deleteDatabaseReference");
    }
}
