//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;


namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    class ProjectDeployParams : DeployParams
    {
        public ProjectBuildInput ProjectBuildInputObject { get; set; }
    }

    class ProjectDeployRequest
    {
        public static readonly RequestType<ProjectDeployParams, DacFxResult> Type =
                  RequestType<ProjectDeployParams, DacFxResult>.Create("dacfx/projectdeploy");
    }
}
