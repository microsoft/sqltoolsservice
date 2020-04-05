//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress build dacpac from project operation
    /// </summary>
    class ProjectBuildOperation : DacFxOperation
    {
        public ProjectBuildParams Parameters { get; }

        public ProjectBuildOperation(ProjectBuildParams parameters) : base(null, connectedOperation: false)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;

            this.Parameters.DatabaseName = this.Parameters.ProjectBuildInputObject?.ProjectName;
        }

        public override void Execute()
        {
            // create profile object -  with pre/post deply and options (null for now)
            // call static DacServices.build with sql files, platform, and build profile

            // Dummy create for now
            File.Copy(@"C:\Users\udgautam\source\repos\Database7\Database1\bin\Debug\Database1.dacpac", Parameters.PackageFilePath);
        }
    }
}
