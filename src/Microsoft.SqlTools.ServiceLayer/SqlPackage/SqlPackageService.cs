//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.Data.Tools.Schema.CommandLineTool.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage
{
    /// <summary>
    /// Service for generating SqlPackage command-line strings from structured parameters.
    /// </summary>
    public class SqlPackageService
    {
        private const string ActionFieldName = "Action";

        private static readonly Lazy<SqlPackageService> instance = new Lazy<SqlPackageService>(() => new SqlPackageService());
        public static SqlPackageService Instance => instance.Value;

        private static readonly IDictionary<CommandLineToolAction, Action<SqlPackageCommandParams, SqlPackageCommandBuilder>> _actionAppliers
            = new Dictionary<CommandLineToolAction, Action<SqlPackageCommandParams, SqlPackageCommandBuilder>>
            {
                {
                    CommandLineToolAction.Publish, (p, b) => ApplyDeployOptions(p, b, true)
                },
                {
                    CommandLineToolAction.Script, (p, b) => ApplyDeployOptions(p, b, true)
                },
                {
                    CommandLineToolAction.DeployReport, (p, b) => ApplyDeployOptions(p, b, false)
                },
                {
                    CommandLineToolAction.Extract, (p, b) =>
                    {
                        if (p.ExtractOptions != null)
                        {
                            b.WithExtractOptions(p.ExtractOptions);
                        }
                    }
                },
                {
                    CommandLineToolAction.Export, (p, b) =>
                    {
                        if (p.ExportOptions != null)
                        {
                            b.WithExportOptions(p.ExportOptions);
                        }
                    }
                },
                {
                    CommandLineToolAction.Import, (p, b) =>
                    {
                        if (p.ImportOptions != null)
                        {
                            b.WithImportOptions(p.ImportOptions);
                        }
                    }
                },
            };

        /// <summary>
        /// Applies deployment options to the SqlPackageCommandBuilder.
        /// </summary>
        /// <param name="p">Parameters containing deployment options</param>
        /// <param name="b">Builder to apply options to</param>
        /// <param name="normalizeDefaults">Whether to normalize STS-overridden defaults to DacFx native defaults</param>
        private static void ApplyDeployOptions(SqlPackageCommandParams p, SqlPackageCommandBuilder b, bool normalizeDefaults)
        {
            if (p.DeploymentOptions != null)
            {
                if (normalizeDefaults) p.DeploymentOptions.NormalizePublishDefaults();
                b.WithDeployOptions(DacFxUtils.CreateDeploymentOptions(p.DeploymentOptions));
            }
        }

        /// <summary>
        /// Initializes the SqlPackage service by registering request handlers with the service host.
        /// </summary>
        /// <param name="serviceHost">The service host to register handlers with</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(GenerateSqlPackageCommandRequest.Type, this.HandleGenerateSqlPackageCommandRequest, isParallelProcessingSupported: true);
        }

        /// <summary>
        /// Handles requests to generate SqlPackage command-line strings.
        /// Maps STS command-line arguments to DacFx types, applies action-specific options,
        /// and builds the final command string using SqlPackageCommandBuilder.
        /// </summary>
        /// <param name="parameters">Parameters containing command-line arguments and action-specific options</param>
        /// <param name="requestContext">Context for sending the result back to the client</param>
        /// <returns>Task representing the async operation</returns>
        public async Task HandleGenerateSqlPackageCommandRequest(
            SqlPackageCommandParams parameters,
            RequestContext<SqlPackageCommandResult> requestContext)
        {
            try
            {
                if (parameters == null || parameters.CommandLineArguments == null)
                {
                    await requestContext.SendResult(new SqlPackageCommandResult
                    {
                        Success = false,
                        ErrorMessage = SR.SqlPackageInvalidRequestParameters
                    });
                    return;
                }

                // Ensure nested object exists; avoids NREs downstream
                parameters.CommandLineArguments.CommandLineProperties = 
                    parameters.CommandLineArguments.CommandLineProperties ?? new CommandLineProperty();

                // Builder fluent API - no mapping needed since SqlPackageCommandLineArguments inherits from DacFx type
                var builder = new SqlPackageCommandBuilder()
                    .WithArguments(parameters.CommandLineArguments)
                    .WithVariables(parameters.Variables);

                // Apply masking configuration from parameters
                if (parameters.MaskMode != default(MaskMode))
                {
                    builder.WithMasking(parameters.MaskMode);
                }

                // Action-specific options via strategy table (no switch noise)
                ApplyActionSpecificOptions(parameters.CommandLineArguments.Action, parameters, builder);

                // Build command — validation exceptions are collected by builder
                var command = builder.Build().ToString();

                await requestContext.SendResult(new SqlPackageCommandResult
                {
                    Command = command,
                    Success = true,
                    ErrorMessage = string.Empty
                });
            }
            catch (SqlPackageCommandException ex)
            {
                Logger.Error($"SqlPackage command validation failed: {ex.Message}");
                await requestContext.SendResult(new SqlPackageCommandResult
                {
                    Command = null,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
            catch (Exception e)
            {
                Logger.Error($"SqlPackage GenerateCommand failed: {e.Message}");
                await requestContext.SendResult(new SqlPackageCommandResult
                {
                    Command = null,
                    Success = false,
                    ErrorMessage = e.Message
                });
            }
        }

        /// <summary>
        /// Applies action-specific options to the SqlPackageCommandBuilder.
        /// Uses strategy pattern to call the appropriate With*Options() method based on action type:
        /// - Publish/Script → b.WithDeployOptions(deployOptions)
        /// - Extract → b.WithExtractOptions(extractOptions)
        /// - Export → b.WithExportOptions(exportOptions)
        /// - Import → b.WithImportOptions(importOptions)
        /// </summary>
        private static void ApplyActionSpecificOptions(CommandLineToolAction action, SqlPackageCommandParams parameters, SqlPackageCommandBuilder builder)
        {
            Action<SqlPackageCommandParams, SqlPackageCommandBuilder>? applier;
            if (_actionAppliers.TryGetValue(action, out applier))
            {
                applier(parameters, builder);
            }
        }
    }
}
