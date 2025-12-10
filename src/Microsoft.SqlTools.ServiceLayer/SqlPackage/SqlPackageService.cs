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
        /// Initializes the SqlPackage service by registering request handlers with the service host.
        /// This method sets up the handler for generating SqlPackage CLI command strings.
        /// </summary>
        /// <param name="serviceHost">The service host that manages RPC communication with clients</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(GenerateSqlPackageCommandRequest.Type, this.HandleGenerateSqlPackageCommandRequest, true);
        }

        /// <summary>
        /// Handles requests to generate SqlPackage CLI command strings for various database operations.
        /// Supports Publish, Extract, Script, Export, and Import actions. The method routes to the appropriate
        /// command generator based on the specified action and uses the SqlPackage API to build properly formatted
        /// command strings with all necessary parameters and options.
        /// </summary>
        /// <param name="parameters">Parameters containing the action type and operation-specific settings (connection strings, file paths, deployment options, etc.)</param>
        /// <param name="requestContext">The request context for sending the command result back to the client</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task HandleGenerateSqlPackageCommandRequest(GenerateSqlPackageCommandParams parameters, RequestContext<SqlPackageCommandResult> requestContext)
        {
            try
            {
                string command;

                switch (parameters.Action)
                {
                    case CommandLineToolAction.Publish:
                        command = GeneratePublishCommand(parameters);
                        break;

                    case CommandLineToolAction.Extract:
                        command = GenerateExtractCommand(parameters);
                        break;

                    case CommandLineToolAction.Script:
                        command = GenerateScriptCommand(parameters);
                        break;

                    case CommandLineToolAction.Export:
                        command = GenerateExportCommand(parameters);
                        break;

                    case CommandLineToolAction.Import:
                        command = GenerateImportCommand(parameters);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported action: {parameters.Action}");
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

        /// <summary>
        /// Generates a SqlPackage Publish command string that deploys a .dacpac file to a target database.
        /// </summary>
        /// <param name="parameters">Parameters including serialized Arguments, DeploymentOptions, and Variables</param>
        /// <returns>A formatted SqlPackage Publish command string with all specified options</returns>
        private string GeneratePublishCommand(GenerateSqlPackageCommandParams parameters)
        {
            DacDeployOptions dacOptions = null;
            if (parameters.DeploymentOptions != null)
            {
                dacOptions = DacFxUtils.CreateDeploymentOptions(parameters.DeploymentOptions);
            }

            return SqlPackageCommandBuilder.GeneratePublishCommand(
                parameters.Arguments,
                dacOptions,
                parameters.Variables);
        }

        /// <summary>
        /// Generates a SqlPackage Extract command string that creates a .dacpac file from a live database.
        /// </summary>
        /// <param name="parameters">Parameters including serialized Arguments and ExtractOptions</param>
        /// <returns>A formatted SqlPackage Extract command string with all specified options</returns>
        private string GenerateExtractCommand(GenerateSqlPackageCommandParams parameters)
        {
            return SqlPackageCommandBuilder.GenerateExtractCommand(
                parameters.Arguments,
                parameters.ExtractOptions);
        }

        /// <summary>
        /// Generates a SqlPackage Script command string that creates a SQL deployment script without executing it.
        /// </summary>
        /// <param name="parameters">Parameters including serialized Arguments, DeploymentOptions, and Variables</param>
        /// <returns>A formatted SqlPackage Script command string with all specified options</returns>
        private string GenerateScriptCommand(GenerateSqlPackageCommandParams parameters)
        {
            DacDeployOptions dacOptions = null;
            if (parameters.DeploymentOptions != null)
            {
                dacOptions = DacFxUtils.CreateDeploymentOptions(parameters.DeploymentOptions);
            }

            return SqlPackageCommandBuilder.GenerateScriptCommand(
                parameters.Arguments,
                dacOptions,
                parameters.Variables);
        }

        /// <summary>
        /// Generates a SqlPackage Export command string that creates a .bacpac file from a live database.
        /// </summary>
        /// <param name="parameters">Parameters including serialized Arguments and ExportOptions</param>
        /// <returns>A formatted SqlPackage Export command string with all specified options</returns>
        private string GenerateExportCommand(GenerateSqlPackageCommandParams parameters)
        {
            return SqlPackageCommandBuilder.GenerateExportCommand(
                parameters.Arguments,
                parameters.ExportOptions);
        }

        /// <summary>
        /// Generates a SqlPackage Import command string that restores a .bacpac file to a target database.
        /// </summary>
        /// <param name="parameters">Parameters including serialized Arguments and ImportOptions</param>
        /// <returns>A formatted SqlPackage Import command string with all specified options</returns>
        private string GenerateImportCommand(GenerateSqlPackageCommandParams parameters)
        {
            return SqlPackageCommandBuilder.GenerateImportCommand(
                parameters.Arguments,
                parameters.ImportOptions);
        }
    }
}