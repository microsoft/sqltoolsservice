//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage
{
    /// <summary>
    /// Main class for SqlPackage service
    /// </summary>
    public class SqlPackageService
    {
        private static readonly Lazy<SqlPackageService> instance = new Lazy<SqlPackageService>(() => new SqlPackageService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SqlPackageService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(GeneratePublishCommandRequest.Type, this.HandleGeneratePublishCommandRequest, true);
        }

        /// <summary>
        /// Handles request to generate a SqlPackage publish command string
        /// </summary>
        public async Task HandleGeneratePublishCommandRequest(GeneratePublishCommandParams parameters, RequestContext<GeneratePublishCommandResult> requestContext)
        {
            try
            {
                // Convert DeploymentOptions from VS Code to DacDeployOptions
                DacDeployOptions? dacOptions = null;
                if (parameters.DeploymentOptions != null)
                {
                    dacOptions = DacFxUtils.CreateDeploymentOptions(parameters.DeploymentOptions);
                }

                // Generate SqlPackage command string
                string commandString = GeneratePublishCommand(
                    sourceFile: parameters.SourceFile,
                    targetFile: parameters.TargetFile,
                    targetConnectionString: parameters.TargetConnectionString,
                    targetServerName: parameters.TargetServerName,
                    targetDatabaseName: parameters.TargetDatabaseName,
                    profilePath: parameters.ProfilePath,
                    deployOptions: dacOptions,
                    variables: parameters.Variables,
                    includeExecutableName: parameters.IncludeExecutableName
                );

                await requestContext.SendResult(new GeneratePublishCommandResult()
                {
                    Command = commandString,
                    Success = true,
                    ErrorMessage = string.Empty
                });
            }
            catch (Exception e)
            {
                Logger.Error($"SqlPackage GeneratePublishCommand failed: {e.Message}");
                await requestContext.SendResult(new GeneratePublishCommandResult()
                {
                    Command = null,
                    Success = false,
                    ErrorMessage = e.Message
                });
            }
        }

        /// <summary>
        /// Generates a SqlPackage.exe publish command string from the provided parameters
        /// Uses Microsoft.SqlPackage NuGet API (SqlPackageCommandBuilder.GeneratePublishCommand)
        /// </summary>
        private string GeneratePublishCommand(
            string sourceFile,
            string targetFile = null,
            string targetConnectionString = null,
            string targetServerName = null,
            string targetDatabaseName = null,
            string profilePath = null,
            DacDeployOptions deployOptions = null,
            Dictionary<string, string> variables = null,
            bool includeExecutableName = true)
        {
            // Use SqlPackage NuGet API to generate the command
            string command = SqlPackageCommandBuilder.GeneratePublishCommand(
                sourceFile: sourceFile,
                targetConnectionString: targetConnectionString,
                targetServerName: targetServerName,
                targetDatabaseName: targetDatabaseName,
                profilePath: profilePath,
                deployOptions: deployOptions,
                variables: variables
            );

            // Add or remove executable name based on parameter
            if (!includeExecutableName && command.StartsWith("sqlpackage ", StringComparison.OrdinalIgnoreCase))
            {
                command = command.Substring("sqlpackage ".Length);
            }
            else if (includeExecutableName && !command.StartsWith("sqlpackage ", StringComparison.OrdinalIgnoreCase))
            {
                command = "sqlpackage " + command;
            }

            return command;
        }
    }
}