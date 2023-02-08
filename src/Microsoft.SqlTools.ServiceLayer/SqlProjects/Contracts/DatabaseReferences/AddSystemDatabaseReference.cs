//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for adding a reference to a system database
    /// </summary>
    public class AddSystemDatabaseReferenceParams : AddDatabaseReferenceParams
    {
        /// <summary>
        /// Type of system database
        /// </summary>
        public SystemDatabase SystemDatabase { get; set; }
    }

    public class AddSystemDatabaseReferenceRequest
    {
        public static readonly RequestType<SqlProjectScriptParams, ResultStatus> Type = RequestType<SqlProjectScriptParams, ResultStatus>.Create("sqlprojects/addSystemDatabaseReference");
    }
}
