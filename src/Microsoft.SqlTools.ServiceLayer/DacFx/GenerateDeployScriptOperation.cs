//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress generate deploy script operation
    /// </summary>
    class GenerateDeployScriptOperation : DacFxOperation
    {
        public GenerateDeployScriptParams Parameters { get; }

        public GenerateDeployScriptOperation(GenerateDeployScriptParams parameters, SqlConnection sqlConnection) : base(sqlConnection)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
            DacPackage dacpac = DacPackage.Load(this.Parameters.PackageFilePath);
            string incrementalScript = this.DacServices.GenerateDeployScript(dacpac, this.Parameters.DatabaseName, null, this.CancellationToken);
            File.WriteAllText(this.Parameters.ScriptFilePath, incrementalScript);
        }
    }
}
