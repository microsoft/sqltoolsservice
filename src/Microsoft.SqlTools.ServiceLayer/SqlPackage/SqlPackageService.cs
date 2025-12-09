//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
            serviceHost.SetRequestHandler(GenerateSqlPackageCommandRequest.Type, this.HandleGenerateSqlPackageCommandRequest, true);
        }

        /// <summary>
        /// Handles request to generate a SqlPackage command string based on action
        /// </summary>
        public async Task HandleGenerateSqlPackageCommandRequest(GenerateSqlPackageCommandParams parameters, RequestContext<SqlPackageCommandResult> requestContext)
        {
            try
            {
                string command;
                string action = parameters.Action?.ToLowerInvariant() ?? string.Empty;

                switch (action)
                {
                    case "publish":
                        command = GeneratePublishCommand(parameters);
                        break;

                    case "extract":
                        command = GenerateExtractCommand(parameters);
                        break;

                    case "script":
                        command = GenerateScriptCommand(parameters);
                        break;

                    case "export":
                        command = GenerateExportCommand(parameters);
                        break;

                    case "import":
                        command = GenerateImportCommand(parameters);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported action: {parameters.Action}. Supported actions are: Publish, Extract, Script, Export, Import");
                }

                // Always include executable name
                if (!command.StartsWith("sqlpackage ", StringComparison.OrdinalIgnoreCase))
                {
                    command = "sqlpackage " + command;
                }

                await requestContext.SendResult(new SqlPackageCommandResult()
                {
                    Command = command,
                    Success = true,
                    ErrorMessage = string.Empty
                });
            }
            catch (Exception e)
            {
                Logger.Error($"SqlPackage Generate{parameters.Action}Command failed: {e.Message}");
                await requestContext.SendResult(new SqlPackageCommandResult()
                {
                    Command = null,
                    Success = false,
                    ErrorMessage = e.Message
                });
            }
        }

        private string GeneratePublishCommand(GenerateSqlPackageCommandParams parameters)
        {
            DacDeployOptions? dacOptions = null;
            if (parameters.DeploymentOptions != null)
            {
                dacOptions = DacFxUtils.CreateDeploymentOptions(parameters.DeploymentOptions);
            }

            return SqlPackageCommandBuilder.GeneratePublishCommand(
                sourceFile: parameters.SourceFile,
                targetConnectionString: parameters.TargetConnectionString,
                targetServerName: parameters.TargetServerName,
                targetDatabaseName: parameters.TargetDatabaseName,
                profilePath: parameters.ProfilePath,
                deployOptions: dacOptions,
                variables: parameters.Variables
            );
        }

        private string GenerateExtractCommand(GenerateSqlPackageCommandParams parameters)
        {
            return SqlPackageCommandBuilder.GenerateExtractCommand(
                sourceConnectionString: parameters.SourceConnectionString,
                targetFile: parameters.TargetFile,
                sourceServerName: parameters.SourceServerName,
                sourceDatabaseName: parameters.SourceDatabaseName,
                applicationName: parameters.ApplicationName,
                applicationVersion: parameters.ApplicationVersion,
                properties: parameters.Properties
            );
        }

        private string GenerateScriptCommand(GenerateSqlPackageCommandParams parameters)
        {
            DacDeployOptions? dacOptions = null;
            if (parameters.DeploymentOptions != null)
            {
                dacOptions = DacFxUtils.CreateDeploymentOptions(parameters.DeploymentOptions);
            }

            return SqlPackageCommandBuilder.GenerateScriptCommand(
                sourceFile: parameters.SourceFile,
                targetFile: parameters.TargetFile,
                targetConnectionString: parameters.TargetConnectionString,
                targetServerName: parameters.TargetServerName,
                targetDatabaseName: parameters.TargetDatabaseName,
                profilePath: parameters.ProfilePath,
                deployOptions: dacOptions,
                variables: parameters.Variables
            );
        }

        private string GenerateExportCommand(GenerateSqlPackageCommandParams parameters)
        {
            return SqlPackageCommandBuilder.GenerateExportCommand(
                sourceConnectionString: parameters.SourceConnectionString,
                targetFile: parameters.TargetFile,
                sourceServerName: parameters.SourceServerName,
                sourceDatabaseName: parameters.SourceDatabaseName,
                properties: parameters.Properties
            );
        }

        private string GenerateImportCommand(GenerateSqlPackageCommandParams parameters)
        {
            return SqlPackageCommandBuilder.GenerateImportCommand(
                sourceFile: parameters.SourceFile,
                targetConnectionString: parameters.TargetConnectionString,
                targetServerName: parameters.TargetServerName,
                targetDatabaseName: parameters.TargetDatabaseName,
                properties: parameters.Properties
            );
        }
    }
}