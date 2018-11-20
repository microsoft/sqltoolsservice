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

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress extract operation
    /// </summary>
    class ExtractOperation : DacFxOperation
    {
        public ExtractParams Parameters { get; }

        public ExtractOperation(ExtractParams parameters, SqlConnection sqlConnection): base(sqlConnection)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
            this.dacServices.Extract(this.Parameters.PackageFilePath, this.Parameters.SourceDatabaseName, this.Parameters.ApplicationName, this.Parameters.ApplicationVersion, null, null, null, this.CancellationToken);
        }
    }
}
