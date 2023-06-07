//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for adding a Dacpac reference to a SQL project
    /// </summary>
    public class AddDacpacReferenceParams : AddUserDatabaseReferenceParams
    {
        /// <summary>
        /// Path to the .dacpac file
        /// </summary>
        public string DacpacPath { get; set; }
    }

    /// <summary>
    /// Add a dacpac reference to a project
    /// </summary>
    public class AddDacpacReferenceRequest
    {
        public static readonly RequestType<AddDacpacReferenceParams, ResultStatus> Type = RequestType<AddDacpacReferenceParams, ResultStatus>.Create("sqlprojects/addDacpacReference");
    }
}