//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public class NewSqlProjectParams : SqlProjectParams
    {
        public ProjectType SqlProjectType { get; set; }

        public string? DspVersion { get; set; }
    }

    public class NewSqlProjectRequest
    {
        public static readonly RequestType<NewSqlProjectParams, SqlProjectResult> Type = RequestType<NewSqlProjectParams, SqlProjectResult>.Create("sqlprojects/new");
    }
}
