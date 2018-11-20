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
    /// Class to represent an in-progress export operation
    /// </summary>
    class ExportOperation : DacFxOperation
    {
        public ExportParams Parameters { get; }

        public ExportOperation(ExportParams parameters, SqlConnection sqlConnection): base(sqlConnection)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
            this.dacServices.ExportBacpac(this.Parameters.PackageFilePath, this.Parameters.SourceDatabaseName, null, this.CancellationToken);
        }
    }
}
