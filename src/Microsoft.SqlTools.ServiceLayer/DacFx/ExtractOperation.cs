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
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress extract operation
    /// </summary>
    class ExtractOperation : DacFxOperation
    {
        public ExtractParams Parameters { get; }

        public ExtractOperation(ExtractParams parameters, ConnectionInfo connInfo) : base(connInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
            Version version = ParseVersion(this.Parameters.ApplicationVersion);
            DacExtractOptions extractOptions = GetExtractOptions(this.Parameters.ExtractTarget);
            this.DacServices.Extract(this.Parameters.PackageFilePath, this.Parameters.DatabaseName, this.Parameters.ApplicationName, version, applicationDescription:null, tables:null, extractOptions:extractOptions, cancellationToken:this.CancellationToken);
        }

        public static Version ParseVersion(string incomingVersion)
        {
            Version parsedVersion;
            if (!Version.TryParse(incomingVersion, out parsedVersion))
            {
                throw new ArgumentException(string.Format(SR.ExtractInvalidVersion, incomingVersion));
            }

            return parsedVersion;
        }

        private DacExtractOptions GetExtractOptions(DacExtractTarget extractTarget)
        {
            try
            {
                // Set diagnostics logging
                DacFxUtils utils = new DacFxUtils();
                utils.SetUpDiagnosticsLogging(this.Parameters.DiagnosticsLogFilePath, this.DacServices);

                DacExtractOptions extractOptions = new DacExtractOptions() { ExtractTarget = extractTarget };
                return extractOptions;
            }
            finally
            {
                // Remove the diagnostic tracer for the current operation based on Name:path
                DacFxUtils utils = new DacFxUtils();
                utils.RemoveDiagnosticListener(this.Parameters.DiagnosticsLogFilePath, this.DacServices);
            }
        }
    }
}
