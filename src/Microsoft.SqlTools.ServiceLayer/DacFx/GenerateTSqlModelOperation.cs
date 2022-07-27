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
    /// Class to represent creating a dacfx model
    /// </summary>
    class GenerateTSqlModelOperation
    {
        public GenerateTSqlModelParams Parameters { get; }

        public GenerateTSqlModelOperation(GenerateTSqlModelParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        /// <summary>
        /// Generate model from sql files, if no sql files are passed in then it generates an empty model.
        /// </summary>
        public TSqlModel GenerateTSqlModel()
        {
            try
            {
                TSqlModelOptions options = new TSqlModelOptions();
                SqlServerVersion version = (SqlServerVersion)Enum.Parse(typeof(SqlServerVersion), Parameters.ModelTargetVersion);

                var model = new TSqlModel(version, options);
                // read all sql files
                foreach (string filePath in Parameters.FilePaths)
                {
                    string fileContent = System.IO.File.ReadAllText(filePath);
                    model.AddOrUpdateObjects(fileContent, filePath, null);
                }
                return model;
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Information, $"Failed to generate model. Error: {ex.Message}");
                throw;
            }
        }
    }
}