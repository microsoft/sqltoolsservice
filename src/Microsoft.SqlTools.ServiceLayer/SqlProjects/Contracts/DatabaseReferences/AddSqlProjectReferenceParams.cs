//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class AddSqlProjectReferenceParams : AddDatabaseReferenceParams
    {
        public string ProjectPath { get; set; }
        public string? ProjectGuid { get; set; }
        public string? ServerVariable { get; set; }
    }


    public class AddSqlProjectReferenceRequest
    {
        public static readonly RequestType<AddSqlProjectReferenceParams, ResultStatus> Type = RequestType<AddSqlProjectReferenceParams, ResultStatus>.Create("sqlprojects/addSqlProjectReference");
    }
}