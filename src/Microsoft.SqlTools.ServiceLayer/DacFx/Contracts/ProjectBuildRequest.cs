//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    public class ProjectBuildParams : IRequestParams
    {
        public string SqlProjectPath { get; set; }

        public string DotNetRootPath { get; set; }

        public string OwnerUri { get; set; } = null;
    }

    public class ProjectBuildRequest
    {
        public static readonly RequestType<ProjectBuildParams, DacFxResult> Type =
    RequestType<ProjectBuildParams, DacFxResult>.Create("databaseProject/build");

    }
}
