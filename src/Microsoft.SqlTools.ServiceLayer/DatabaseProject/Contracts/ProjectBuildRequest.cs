//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    public class ProjectBuildParams : DacFxParams
    {
        public ProjectBuildInput ProjectBuildInputObject { get; set; }
    }

    public class ProjectBuildInput
    {
        public string ProjectName { get; set; }
         
        public string SqlProjectPath { get; set; }
    }

    public class ProjectBuildRequest
    {
        public static readonly RequestType<ProjectBuildParams, DacFxResult> Type =
    RequestType<ProjectBuildParams, DacFxResult>.Create("dacfx/projectbuild");

    }
}
