//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress deploy operation
    /// </summary>
    class ProjectDeployOperation : DacFxOperation
    {
        public ProjectDeployParams Paramters { get; }

        protected ProjectBuildOperation Build { get; set; }

        protected DeployOperation Deploy { get; set; }

        public ProjectDeployOperation(ProjectDeployParams parameters, ConnectionInfo connInfo) : base(connInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Paramters = parameters;

            ProjectBuildParams buildParams = new ProjectBuildParams() { PackageFilePath = parameters.PackageFilePath, ProjectBuildInputObject = parameters.ProjectBuildInputObject };
            Build = new ProjectBuildOperation(buildParams);

            Deploy = new DeployOperation(parameters, connInfo);
        }

        public override void Execute()
        {
            Build.Execute(TaskExecutionMode.Execute);
            Deploy.Execute(TaskExecutionMode.Execute);
        }
    }
}
