//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.SqlCore.DacFx;
using Microsoft.SqlTools.SqlCore.DacFx.Contracts;
using Microsoft.SqlTools.Utility;
using DacTableDesigner = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner.TableDesigner;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Main class for DacFx service
    /// </summary>
    class DacFxService
    {
        private static readonly Lazy<DacFxService> instance = new Lazy<DacFxService>(() => new DacFxService());
        private static Version? serviceVersion = LoadServiceVersion();
        private const string TelemetryDefaultApplicationName = "sqltoolsservice";
        private string telemetryApplicationName = TelemetryDefaultApplicationName;
        private readonly Lazy<ConcurrentDictionary<string, DacFxOperation>> operations =
            new Lazy<ConcurrentDictionary<string, DacFxOperation>>(() => new ConcurrentDictionary<string, DacFxOperation>());
        /// <summary>
        /// <see cref="ConcurrentDictionary{String, TSqlModel}"/> that maps project uri to model
        /// </summary>
        public Lazy<ConcurrentDictionary<string, TSqlModel>> projectModels =
            new Lazy<ConcurrentDictionary<string, TSqlModel>>(() => new ConcurrentDictionary<string, TSqlModel>());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static DacFxService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost, ServiceLayerCommandOptions commandOptions)
        {
            serviceHost.RegisterRequestHandler(ExportRequest.Type, this.HandleExportRequest);
            serviceHost.RegisterRequestHandler(ImportRequest.Type, this.HandleImportRequest);
            serviceHost.RegisterRequestHandler(ExtractRequest.Type, this.HandleExtractRequest);
            serviceHost.RegisterRequestHandler(DeployRequest.Type, this.HandleDeployRequest);
            serviceHost.RegisterRequestHandler(GenerateDeployScriptRequest.Type, this.HandleGenerateDeployScriptRequest);
            serviceHost.RegisterRequestHandler(GenerateDeployPlanRequest.Type, this.HandleGenerateDeployPlanRequest);
            serviceHost.RegisterRequestHandler(GetOptionsFromProfileRequest.Type, this.HandleGetOptionsFromProfileRequest);
            serviceHost.RegisterRequestHandler(ValidateStreamingJobRequest.Type, this.HandleValidateStreamingJobRequest);
            serviceHost.RegisterRequestHandler(GetDefaultPublishOptionsRequest.Type, this.HandleGetDefaultPublishOptionsRequest);
            serviceHost.RegisterRequestHandler(GetDeploymentOptionsRequest.Type, this.HandleGetDeploymentOptionsRequest);
            serviceHost.RegisterRequestHandler(ParseTSqlScriptRequest.Type, this.HandleParseTSqlScriptRequest);
            serviceHost.RegisterRequestHandler(GenerateTSqlModelRequest.Type, this.HandleGenerateTSqlModelRequest);
            serviceHost.RegisterRequestHandler(GetObjectsFromTSqlModelRequest.Type, this.HandleGetObjectsFromTSqlModelRequest);
            serviceHost.RegisterRequestHandler(SavePublishProfileRequest.Type, this.HandleSavePublishProfileRequest);
            serviceHost.RegisterRequestHandler(GetCodeAnalysisRulesRequest.Type, this.HandleGetCodeAnalysisRulesRequest);
            Workspace.WorkspaceService<SqlToolsSettings>.Instance.RegisterConfigChangeCallback(UpdateSettings);
            telemetryApplicationName = string.IsNullOrEmpty(commandOptions?.ApplicationName) ? TelemetryDefaultApplicationName : commandOptions.ApplicationName;
        }

        internal Task UpdateSettings(SqlToolsSettings newSettings, SqlToolsSettings oldSettings)
        {
            // Update telemetry status in DacFx service
            UpdateTelemetryStatus(newSettings.TelemetrySettings.Telemetry != TelemetryLevel.Off);
            return Task.CompletedTask;
        }

        /// <summary>
        /// The collection of active operations
        /// </summary>
        internal ConcurrentDictionary<string, DacFxOperation> ActiveOperations => operations.Value;

        /// <summary>
        /// Handles request to export a bacpac
        /// </summary>
        /// <returns></returns>
        public Task<DacFxResult> HandleExportRequest(ExportParams parameters)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                ExportOperation operation = new ExportOperation(parameters, connInfo);
                return ExecuteOperation(operation, parameters, SR.ExportBacpacTaskName);
            }
            return Task.FromResult(new DacFxResult { Success = false, ErrorMessage = SR.QueryServiceQueryInvalidOwnerUri });
        }

        /// <summary>
        /// Handles request to import a bacpac
        /// </summary>
        /// <returns></returns>
        public Task<DacFxResult> HandleImportRequest(ImportParams parameters)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                ImportOperation operation = new ImportOperation(parameters, connInfo);
                return ExecuteOperation(operation, parameters, SR.ImportBacpacTaskName);
            }
            return Task.FromResult(new DacFxResult { Success = false, ErrorMessage = SR.QueryServiceQueryInvalidOwnerUri });
        }

        /// <summary>
        /// Handles request to extract a dacpac
        /// </summary>
        /// <returns></returns>
        public Task<DacFxResult> HandleExtractRequest(ExtractParams parameters)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                // Set connection details database name to ensure the connection string gets created correctly for DW(extract doesn't work if connection is to master)
                connInfo.ConnectionDetails.DatabaseName = parameters.DatabaseName;
                ExtractOperation operation = new ExtractOperation(parameters, connInfo);
                string taskName = parameters.ExtractTarget == DacExtractTarget.DacPac ? SR.ExtractDacpacTaskName : SR.ProjectExtractTaskName;
                return ExecuteOperation(operation, parameters, taskName);
            }
            return Task.FromResult(new DacFxResult { Success = false, ErrorMessage = SR.QueryServiceQueryInvalidOwnerUri });
        }

        /// <summary>
        /// Handles request to deploy a dacpac
        /// </summary>
        /// <returns></returns>
        public Task<DacFxResult> HandleDeployRequest(DeployParams parameters)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                DeployOperation operation = new DeployOperation(parameters, connInfo);
                return ExecuteOperation(operation, parameters, SR.DeployDacpacTaskName);
            }
            return Task.FromResult(new DacFxResult { Success = false, ErrorMessage = SR.QueryServiceQueryInvalidOwnerUri });
        }

        /// <summary>
        /// Handles request to generate deploy script
        /// </summary>
        /// <returns></returns>
        public async Task<DacFxResult> HandleGenerateDeployScriptRequest(GenerateDeployScriptParams parameters)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                GenerateDeployScriptOperation operation = new GenerateDeployScriptOperation(parameters, connInfo);
                TaskMetadata metadata = new TaskMetadata();
                metadata.TaskOperation = operation;
                metadata.TaskExecutionMode = parameters.TaskExecutionMode;
                metadata.ServerName = connInfo.ConnectionDetails.ServerName;
                metadata.DatabaseName = parameters.DatabaseName;
                metadata.Name = SR.GenerateScriptTaskName;

                operation.SqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                return new DacFxResult()
                {
                    OperationId = operation.OperationId,
                    Success = true,
                    ErrorMessage = string.Empty
                };
            }

            return new DacFxResult { Success = false, ErrorMessage = SR.QueryServiceQueryInvalidOwnerUri };
        }

        /// <summary>
        /// Handles request to generate deploy plan
        /// </summary>
        /// <returns></returns>
        public async Task<GenerateDeployPlanRequestResult> HandleGenerateDeployPlanRequest(GenerateDeployPlanParams parameters)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                return await BaseService.RunWithErrorHandling(() =>
                {
                    GenerateDeployPlanOperation operation = new GenerateDeployPlanOperation(parameters, connInfo);
                    operation.Execute(parameters.TaskExecutionMode);

                    return new GenerateDeployPlanRequestResult()
                    {
                        OperationId = operation.OperationId,
                        Success = true,
                        ErrorMessage = string.Empty,
                        Report = operation.DeployReport
                    };
                });
            }

            return new GenerateDeployPlanRequestResult { Success = false, ErrorMessage = SR.QueryServiceQueryInvalidOwnerUri };
        }

        /// <summary>
        /// Handles request to get the options from a publish profile
        /// </summary>
        /// <returns></returns>
        public async Task<DacFxOptionsResult> HandleGetOptionsFromProfileRequest(GetOptionsFromProfileParams parameters)
        {
            DeploymentOptions? options = null;
            if (parameters.ProfilePath != null)
            {
                DacProfile profile = DacProfile.Load(parameters.ProfilePath);
                if (profile.DeployOptions != null)
                {
                    options = DeploymentOptions.GetDefaultPublishOptions();
                    await options.InitializeFromProfile(profile.DeployOptions, parameters.ProfilePath);
                }
            }

            return new DacFxOptionsResult()
            {
                DeploymentOptions = options,
                Success = true,
                ErrorMessage = string.Empty,
            };
        }

        /// <summary>
        /// Handles request to validate an ASA streaming job
        /// </summary>
        /// <returns></returns>
        public async Task<ValidateStreamingJobResult> HandleValidateStreamingJobRequest(ValidateStreamingJobParams parameters)
        {
            ValidateStreamingJobOperation operation = new ValidateStreamingJobOperation(parameters);
            ValidateStreamingJobResult result = operation.ValidateQuery();

            return result;
        }

        /// <summary>
        /// Handles request to create default publish options for DacFx
        /// </summary>
        /// <returns></returns>
        public async Task<DacFxOptionsResult> HandleGetDefaultPublishOptionsRequest(GetDefaultPublishOptionsParams parameters)
        {
            try
            {
                // this does not need to be an async operation since this only creates and returns the default object
                DeploymentOptions options = DeploymentOptions.GetDefaultPublishOptions();

                return new DacFxOptionsResult()
                {
                    DeploymentOptions = options,
                    Success = true,
                    ErrorMessage = null
                };
            }
            catch (Exception e)
            {
                return new DacFxOptionsResult()
                {
                    DeploymentOptions = null,
                    Success = false,
                    ErrorMessage = e.Message
                };
            }
        }

        /// <summary>
        /// Gets deployment options based on the specified scenario
        /// </summary>
        public async Task<GetDeploymentOptionsResult> HandleGetDeploymentOptionsRequest(GetDeploymentOptionsParams parameters)
        {
            try
            {
                DeploymentOptions options = new DeploymentOptions(parameters.Scenario);

                return new GetDeploymentOptionsResult()
                {
                    DefaultDeploymentOptions = options,
                    Success = true,
                    ErrorMessage = null
                };
            }
            catch (Exception e)
            {
                return new GetDeploymentOptionsResult()
                {
                    DefaultDeploymentOptions = null,
                    Success = false,
                    ErrorMessage = e.Message
                };
            }
        }

        public async Task<ParseTSqlScriptResult> HandleParseTSqlScriptRequest(ParseTSqlScriptRequestParams requestParams)
        {
            var script = System.IO.File.ReadAllText(requestParams.FilePath);
            return new ParseTSqlScriptResult()
            {
                ContainsCreateTableStatement = DacTableDesigner.ScriptContainsCreateTableStatements(script, requestParams.DatabaseSchemaProvider)
            };
        }

        public async Task<bool> HandleGenerateTSqlModelRequest(GenerateTSqlModelParams requestParams)
        {
            try
            {
                GenerateTSqlModelOperation operation = new GenerateTSqlModelOperation(requestParams);
                TSqlModel model = operation.GenerateTSqlModel();

                projectModels.Value[operation.Parameters.ProjectUri] = model;
                return true;
            }
            catch (Exception e)
            {
                throw RpcErrorException.Create(e);
            }
        }

        /// <summary>
        /// Handles request to get objects from sql model
        /// </summary>
        /// <returns></returns>
        public async Task<TSqlObjectInfo[]> HandleGetObjectsFromTSqlModelRequest(GetObjectsFromTSqlModelParams requestParams)
        {
            TSqlObjectInfo[] objectInfos = { };
            var model = projectModels.Value[requestParams.ProjectUri];

            if (model == null)
            {
                throw RpcErrorException.Create(new Exception(SR.SqlProjectModelNotFound(requestParams.ProjectUri)));
            }
            else
            {
                GetObjectsFromTSqlModelOperation operation = new GetObjectsFromTSqlModelOperation(requestParams, model);
                objectInfos = operation.GetObjectsFromTSqlModel();
                return objectInfos;
            }
        }

        /// <summary>
        /// Handles request to save a publish profile
        /// </summary>
        /// <returns></returns>
        public async Task<ResultStatus> HandleSavePublishProfileRequest(SavePublishProfileParams parameters)
        {
            return await BaseService.RunWithErrorHandling(() =>
            {
                if (parameters.ProfilePath != null)
                {
                    DacProfile profile = new DacProfile();
                    profile.TargetDatabaseName = parameters.DatabaseName;
                    profile.TargetConnectionString = parameters.ConnectionString;
                    profile.DeployOptions = DacFxUtils.CreateDeploymentOptions(parameters.DeploymentOptions);

                    if (parameters.SqlCommandVariableValues != null)
                    {
                        foreach (string key in parameters.SqlCommandVariableValues.Keys)
                        {
                            profile.DeployOptions.SqlCommandVariableValues[key] = parameters.SqlCommandVariableValues[key];
                        }
                    }
                    profile.Save(parameters.ProfilePath);
                }
            });
        }

        /// <summary>
        /// Handles request to get all available built-in SQL code analysis rules.
        /// Creates a minimal TSqlModel to enumerate rules from DacFx CodeAnalysisService.
        /// The rules are static and do not depend on model content or SQL Server version.
        /// </summary>
        public async Task<GetCodeAnalysisRulesResult> HandleGetCodeAnalysisRulesRequest(GetCodeAnalysisRulesParams parameters)
        {
            return await BaseService.RunWithErrorHandling(() =>
            {
                // Version doesn't affect the rules returned; a model is only needed to obtain a CodeAnalysisService instance.
                using var model = new TSqlModel(SqlServerVersion.Sql170, new TSqlModelOptions());
                var factory = new CodeAnalysisServiceFactory();
                var codeAnalysisService = factory.CreateAnalysisService(model);
                var rules = codeAnalysisService.GetRules();

                var ruleInfos = rules.Select(r => new CodeAnalysisRuleInfo
                {
                    RuleId = r.RuleId,
                    ShortRuleId = r.ShortRuleId,
                    DisplayName = r.DisplayName,
                    Description = r.DisplayDescription,
                    Category = r.Metadata?.Category ?? string.Empty,
                    Severity = r.Severity.ToString(),
                    RuleScope = r.Metadata?.RuleScope.ToString() ?? string.Empty
                }).ToArray();

                return new GetCodeAnalysisRulesResult
                {
                    Success = true,
                    ErrorMessage = null,
                    Rules = ruleInfos
                };
            });
        }

        private async Task<DacFxResult> ExecuteOperation(DacFxOperation operation, DacFxParams parameters, string taskName)
        {
            try
            {
                // show file location for export and extract operations 
                string? targetLocation = (operation is ExportOperation || operation is ExtractOperation) ? parameters.PackageFilePath : null;
                TaskMetadata metadata = TaskMetadata.Create(parameters, taskName, operation, ConnectionServiceInstance, targetLocation);

                // put appropriate database name since connection passed was to master
                metadata.DatabaseName = parameters.DatabaseName;
                metadata.OperationName = operation.GetType().Name;
                operation.SqlTask = SqlTaskManagerInstance.CreateTask<SqlTask>(metadata);

                await operation.SqlTask.RunAsync();
                return new DacFxResult()
                {
                    OperationId = operation.OperationId,
                    Success = operation.SqlTask.TaskStatus == SqlTaskStatus.Succeeded,
                    ErrorMessage = string.Empty,
                };
            }
            catch (Exception e)
            {
                return new DacFxResult()
                {
                    OperationId = operation.OperationId,
                    Success = false,
                    ErrorMessage = e.Message,
                };
            }
        }

        private SqlTaskManager SqlTaskManagerInstance
        {
            get
            {
               return SqlTaskManager.Instance;
            }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                return ConnectionService.Instance;
            }
        }

        /// <summary>
        /// For testing purpose only
        /// </summary>
        /// <param name="operation"></param>
        internal void PerformOperation(DacFxOperation operation, TaskExecutionMode taskExecutionMode)
        {
            operation.Execute(taskExecutionMode);
        }

        /// <summary>
        /// Changes telemetry status 
        /// </summary>
        private void UpdateTelemetryStatus(bool telemetryEnabled)
        {
            try
            {
                if (telemetryEnabled)
                {
                    DacServices.EnableTelemetry(telemetryApplicationName, serviceVersion);
                }
                else
                {
                    DacServices.DisableTelemetry();
                }

            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to update DacFx telemetry status. telemetry enable: {telemetryEnabled}, error: {ex.Message}");
            }
        }

        private static Version? LoadServiceVersion()
        {
            try
            {
                string? fileVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
                if (Version.TryParse(fileVersion, out Version? version))
                {
                    return version;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load assembly version:  error: {ex.Message}");
                return null;
            }
        }
    }
}
