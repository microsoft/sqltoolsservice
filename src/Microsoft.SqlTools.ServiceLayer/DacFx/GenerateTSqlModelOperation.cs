//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Dac.Model;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent getting the Azure Functions in a file
    /// </summary>
    class GenerateTSqlModelOperation
    {
        public GenerateTSqlModelParams Parameters { get; }

        public GenerateTSqlModelOperation(GenerateTSqlModelParams parameters)
        {
            Validate.IsNotNull("parameters.ModelTargetVersion", parameters.ModelTargetVersion);
            this.Parameters = parameters;
        }

        /// <summary>
        /// Generate model from sql files, if no sql files are passed in then it generates an empty model.
        /// </summary>
        public GenerateTSqlModelResult GenerateTSqlModel()
        {
            try
            {
                TSqlModelOptions options = new TSqlModelOptions();
                SqlServerVersion version = (SqlServerVersion)Enum.Parse(typeof(SqlServerVersion), Parameters.ModelTargetVersion);

                var model = new TSqlModel(version, options);
                // read all sql files
                if (Parameters.FilePaths?.Length > 0)
                {
                    string[] scripts = new string[Parameters.FilePaths.Length];
                    for (var i = 0; i < Parameters.FilePaths.Length; i++)
                    {
                        scripts[i] = System.IO.File.ReadAllText(Parameters.FilePaths[i]);
                        model.AddOrUpdateObjects(scripts[i], Parameters.FilePaths[i], null);
                    }
                }
                 return new GenerateTSqlModelResult(model);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Information, $"Failed to generate model. Error: {ex.Message}");
                throw;
            }
        }
    }
}