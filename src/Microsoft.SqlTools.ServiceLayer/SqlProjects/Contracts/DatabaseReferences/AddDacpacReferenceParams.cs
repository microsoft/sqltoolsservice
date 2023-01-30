//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class AddDacpacReferenceParams : AddDatabaseReferenceParams
    {
        public string DacpacPath { get; set; }
        public string? ServerVariable { get; set; }
    }

    public class AddDacpacReferenceRequest
    {
        public static readonly RequestType<AddDacpacReferenceParams, ResultStatus> Type = RequestType<AddDacpacReferenceParams, ResultStatus>.Create("sqlprojects/addDacpacReference");
    }
}