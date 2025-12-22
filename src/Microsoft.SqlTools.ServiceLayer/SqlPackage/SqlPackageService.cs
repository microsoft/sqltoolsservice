//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json;

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
        /// Converts DeploymentOptions to DacDeployOptions and delegates to the SqlPackage API.
        /// </summary>
        /// <param name="parameters">Parameters containing the action type and operation-specific settings (connection strings, file paths, deployment options, etc.)</param>
        /// <param name="requestContext">The request context for sending the command result back to the client</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task HandleGenerateSqlPackageCommandRequest(SqlPackageCommandParams parameters, RequestContext<SqlPackageCommandResult> requestContext)
        {
            try
            {
                // Normalize deployment options for Publish/Script operations only.
                // These operations use DeploymentOptions which has 7 STS-overridden defaults that differ from DacFx defaults.
                // Reset them to DacFx defaults so they won't appear as unnecessary /p: parameters in the command.
                // Import/Export/Extract use their own option types (DacImportOptions, DacExportOptions, DacExtractOptions) 
                // which don't have this override issue, so no normalization is needed.
                if (parameters.DeploymentOptions != null && 
                    (parameters.CommandLineArguments.Action == CommandLineToolAction.Publish || parameters.CommandLineArguments.Action == CommandLineToolAction.Script))
                {
                    parameters.DeploymentOptions.NormalizePublishDefaults();
                }

                // Convert STS CommandLineArguments to DacFx CommandLineArguments via JSON serialization
                // Our STS model is a subset of DacFx's CommandLineArguments, so we serialize and deserialize
                // to properly map to the DacFx type expected by the SqlPackage API
                string commandLineArgsJson = JsonConvert.SerializeObject(parameters.CommandLineArguments);
                var dacfxCommandLineArgs = JsonConvert.DeserializeObject<Microsoft.Data.Tools.Schema.CommandLineTool.CommandLineArguments>(commandLineArgsJson);

                // Create API parameters with DacFx CommandLineArguments and converted DeploymentOptions
                var apiParams = new GenerateSqlPackageCommandParams
                {
                    CommandLineArguments = dacfxCommandLineArgs,
                    DeploymentOptions = parameters.DeploymentOptions != null ? DacFxUtils.CreateDeploymentOptions(parameters.DeploymentOptions) : null,
                    ExtractOptions = parameters.ExtractOptions,
                    ExportOptions = parameters.ExportOptions,
                    ImportOptions = parameters.ImportOptions,
                    Variables = parameters.Variables
                };
                
                // Delegate to unified SqlPackage API method
                string command = SqlPackageCommandGenerator.GenerateSqlPackageCommand(apiParams);

                await requestContext.SendResult(new SqlPackageCommandResult()
                {
                    Command = command,
                    Success = true,
                    ErrorMessage = string.Empty
                });
            }
            catch (Exception e)
            {
                Logger.Error($"SqlPackage GenerateCommand failed: {e.Message}");
                await requestContext.SendResult(new SqlPackageCommandResult()
                {
                    Command = null,
                    Success = false,
                    ErrorMessage = e.Message
                });
            }
        }
    }
}