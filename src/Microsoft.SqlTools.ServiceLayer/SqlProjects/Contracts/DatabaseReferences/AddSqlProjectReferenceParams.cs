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
    /// Parameters for adding a reference to another SQL project
    /// </summary>
    public class AddSqlProjectReferenceParams : AddUserDatabaseReferenceParams
    {
        /// <summary>
        /// Path to the referenced .sqlproj file
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// GUID for the referenced SQL project
        /// </summary>
        public string ProjectGuid { get; set; }
    }


    public class AddSqlProjectReferenceRequest
    {
        public static readonly RequestType<AddSqlProjectReferenceParams, ResultStatus> Type = RequestType<AddSqlProjectReferenceParams, ResultStatus>.Create("sqlprojects/addSqlProjectReference");
    }
}