//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.DataCollection.Common;
using Microsoft.SqlServer.DataCollection.Common.Contracts.OperationsInfrastructure;
using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlServer.Management.Assessment.Checks;
using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.Models;
using Microsoft.SqlServer.Migration.Assessment.Common.Engine;
using Microsoft.SqlServer.Migration.Assessment.Common.Models;
using Microsoft.SqlServer.Migration.Assessment.Common.Utils;
using Microsoft.SqlServer.Migration.Logins;
using Microsoft.SqlServer.Migration.Logins.Contracts;
using Microsoft.SqlServer.Migration.Logins.Contracts.ErrorHandling;
using Microsoft.SqlServer.Migration.Logins.ErrorHandling;
using Microsoft.SqlServer.Migration.Logins.Helpers;
using Microsoft.SqlServer.Migration.SkuRecommendation;
using Microsoft.SqlServer.Migration.SkuRecommendation.Aggregation;
using Microsoft.SqlServer.Migration.SkuRecommendation.Billing;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Constants;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Exceptions;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models.Environment;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models.Sku;
using Microsoft.SqlServer.Migration.SkuRecommendation.ElasticStrategy;
using Microsoft.SqlServer.Migration.SkuRecommendation.ElasticStrategy.AzureSqlDatabase;
using Microsoft.SqlServer.Migration.SkuRecommendation.ElasticStrategy.AzureSqlManagedInstance;
using Microsoft.SqlServer.Migration.SkuRecommendation.Models;
using Microsoft.SqlServer.Migration.SkuRecommendation.Models.Sql;
using Microsoft.SqlServer.Migration.SkuRecommendation.TargetProvisioning;
using Microsoft.SqlServer.Migration.SkuRecommendation.Utils;
using Microsoft.SqlServer.Migration.Tde;
using Microsoft.SqlServer.Migration.Tde.Common;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Migration.Contracts;
using Microsoft.SqlTools.Migration.Models;
using Microsoft.SqlTools.Migration.Utils;
using Microsoft.SqlTools.Utility;

using Newtonsoft.Json;

namespace Microsoft.SqlTools.Migration
{
    internal class MigrationService: IHostedService
    {
        private static readonly Lazy<MigrationService> instance = new Lazy<MigrationService>(() => new MigrationService());

        private bool disposed;

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static MigrationService Instance
        {
            get { return instance.Value; }
        }

        /// Gets the <see cref="Engine"/> used to run assessment operations.
        /// </summary>
        internal Engine Engine { get; } = new Engine();

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Controller for collecting performance data for SKU recommendation
        /// </summary>
        internal SqlDataQueryController DataCollectionController
        {
            get;
            set;
        }

        public Type ServiceType => this.GetType();

        /// <summary>
        /// Initializes the Migration Service instance
        /// </summary>
        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(MigrationAssessmentsRequest.Type, HandleMigrationAssessmentsRequest, true);
            this.ServiceHost.SetRequestHandler(StartPerfDataCollectionRequest.Type, HandleStartPerfDataCollectionRequest, true);
            this.ServiceHost.SetRequestHandler(StopPerfDataCollectionRequest.Type, HandleStopPerfDataCollectionRequest, true);
            this.ServiceHost.SetRequestHandler(RefreshPerfDataCollectionRequest.Type, HandleRefreshPerfDataCollectionRequest, true);
            this.ServiceHost.SetRequestHandler(GetSkuRecommendationsRequest.Type, HandleGetSkuRecommendationsRequest, true);
            this.ServiceHost.SetRequestHandler(GenerateProvisioningScriptRequest.Type, HandleGenerateProvisioningScriptRequest, true);
            this.ServiceHost.SetRequestHandler(StartLoginMigrationRequest.Type, HandleStartLoginMigration, true);
            this.ServiceHost.SetRequestHandler(ValidateLoginMigrationRequest.Type, HandleValidateLoginMigration, true);
            this.ServiceHost.SetRequestHandler(MigrateLoginsRequest.Type, HandleMigrateLogins, true);
            this.ServiceHost.SetRequestHandler(EstablishUserMappingRequest.Type, HandleEstablishUserMapping, true);
            this.ServiceHost.SetRequestHandler(MigrateServerRolesAndSetPermissionsRequest.Type, HandleMigrateServerRolesAndSetPermissions, true);
            this.ServiceHost.SetRequestHandler(CertificateMigrationRequest.Type, HandleTdeCertificateMigrationRequest);
            Logger.Verbose("Migration Service initialized");
        }


        /// <summary>
        /// Handle request to start a migration session
        /// </summary>
        internal async Task HandleMigrationAssessmentsRequest(
            MigrationAssessmentsParams parameters,
            RequestContext<MigrationAssessmentResult> requestContext)
        {
            try
            {
                var connectionStrings = new List<string>();
                if (parameters.Databases != null)
                {
                    SqlConnectionStringBuilder connStringBuilder = new SqlConnectionStringBuilder(parameters.ConnectionString);
                    foreach (string database in parameters.Databases)
                    {
                        connStringBuilder.InitialCatalog = database;
                        connectionStrings.Add(connStringBuilder.ConnectionString);
                    }
                    string[] assessmentConnectionStrings = connectionStrings.ToArray();
                    var results = await GetAssessmentItems(assessmentConnectionStrings, parameters.XEventsFilesFolderPath);
                    await requestContext.SendResult(results);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                await requestContext.SendError(e.ToString());
            }
        }

        /// <summary>
        /// Handle request to start performance data collection process
        /// </summary>
        internal async Task HandleStartPerfDataCollectionRequest(
            StartPerfDataCollectionParams parameters,
            RequestContext<StartPerfDataCollectionResult> requestContext)
        {
            string randomUri = Guid.NewGuid().ToString();
            try
            {
                this.DataCollectionController = new SqlDataQueryController(
                    parameters.ConnectionString,
                    parameters.DataFolder,
                    parameters.PerfQueryIntervalInSec,
                    parameters.NumberOfIterations,
                    parameters.StaticQueryIntervalInSec,
                    null);

                this.DataCollectionController.Start();

                // TO-DO: what should be returned?
                await requestContext.SendResult(new StartPerfDataCollectionResult() { DateTimeStarted = DateTime.UtcNow });
            } catch (Exception e){
                Logger.Error(e);
                await requestContext.SendError(e.ToString());
            }
        }

        /// <summary>
        /// Handle request to stop performance data collection process
        /// </summary>
        internal async Task HandleStopPerfDataCollectionRequest(
            StopPerfDataCollectionParams parameters,
            RequestContext<StopPerfDataCollectionResult> requestContext)
        {
            this.DataCollectionController.Dispose();

            // TO-DO: what should be returned?
            await requestContext.SendResult(new StopPerfDataCollectionResult() { DateTimeStopped = DateTime.UtcNow });
        }

        /// <summary>
        /// Handle request to refresh performance data collection status
        /// </summary>
        internal async Task HandleRefreshPerfDataCollectionRequest(
            RefreshPerfDataCollectionParams parameters,
            RequestContext<RefreshPerfDataCollectionResult> requestContext)
        {
            bool isCollecting = !(this.DataCollectionController is null) ? this.DataCollectionController.IsRunning() : false;
            List<string> messages = !(this.DataCollectionController is null) ? this.DataCollectionController.FetchLatestMessages(parameters.LastRefreshedTime) : new List<string>();
            List<string> errors = !(this.DataCollectionController is null) ? this.DataCollectionController.FetchLatestErrors(parameters.LastRefreshedTime) : new List<string>();

            RefreshPerfDataCollectionResult result = new RefreshPerfDataCollectionResult()
            {
                RefreshTime = DateTime.UtcNow,
                IsCollecting = isCollecting,
                Messages = messages,
                Errors = errors,
            };

            await requestContext.SendResult(result);
        }
        /// <summary>
        /// Handle request to generate SKU recommendations
        /// </summary>
        internal async Task HandleGetSkuRecommendationsRequest(
            GetSkuRecommendationsParams parameters,
            RequestContext<GetSkuRecommendationsResult> requestContext)
        {
            try
            {
                SqlAssessmentConfiguration.EnableLocalLogging = true;
                SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath = Path.GetDirectoryName(Logger.LogFileFullPath);

                CsvRequirementsAggregator aggregator = new CsvRequirementsAggregator(parameters.DataFolder);
                SqlInstanceRequirements req = aggregator.ComputeSqlInstanceRequirements(
                    agentId: null,
                    instanceId: parameters.TargetSqlInstance,
                    targetPercentile: parameters.TargetPercentile,
                    startTime: DateTime.ParseExact(parameters.StartTime, RecommendationConstants.TimestampDateTimeFormat, CultureInfo.InvariantCulture),
                    endTime: DateTime.ParseExact(parameters.EndTime, RecommendationConstants.TimestampDateTimeFormat, CultureInfo.InvariantCulture),
                    collectionInterval: parameters.PerfQueryIntervalInSec,
                    dbsToInclude: new HashSet<string>(parameters.DatabaseAllowList),
                    hostRequirements: new SqlServerHostRequirements() { NICCount = 1 });

                RecommendationResultSet baselineResults;
                RecommendationResultSet elasticResults;

                try
                {
                    baselineResults = GenerateBaselineRecommendations(req, parameters);
                }
                catch (Exception e)
                {
                    baselineResults = new RecommendationResultSet();
                }

                try
                {
                    elasticResults = GenerateElasticRecommendations(req, parameters);
                }
                catch (Exception e)
                {
                    elasticResults = new RecommendationResultSet();
                }

                GetSkuRecommendationsResult results = new GetSkuRecommendationsResult
                {
                    SqlDbRecommendationResults = baselineResults.sqlDbResults,
                    SqlDbRecommendationDurationInMs = baselineResults.sqlDbDurationInMs,
                    SqlMiRecommendationResults = baselineResults.sqlMiResults,
                    SqlMiRecommendationDurationInMs = baselineResults.sqlMiDurationInMs,
                    SqlVmRecommendationResults = baselineResults.sqlVmResults,
                    SqlVmRecommendationDurationInMs = baselineResults.sqlVmDurationInMs,
                    ElasticSqlDbRecommendationResults = elasticResults.sqlDbResults,
                    ElasticSqlDbRecommendationDurationInMs = elasticResults.sqlDbDurationInMs,
                    ElasticSqlMiRecommendationResults = elasticResults.sqlMiResults,
                    ElasticSqlMiRecommendationDurationInMs = elasticResults.sqlMiDurationInMs,
                    ElasticSqlVmRecommendationResults = elasticResults.sqlVmResults,
                    ElasticSqlVmRecommendationDurationInMs = elasticResults.sqlVmDurationInMs,
                    InstanceRequirements = req,
                    SkuRecommendationReportPaths = new List<string> { baselineResults.sqlDbReportPath, baselineResults.sqlMiReportPath, baselineResults.sqlVmReportPath },
                    ElasticSkuRecommendationReportPaths = new List<string> { elasticResults.sqlDbReportPath, elasticResults.sqlMiReportPath, elasticResults.sqlVmReportPath },
                };

                await requestContext.SendResult(results);
            }
            catch (FailedToQueryCountersException e)
            {
                await requestContext.SendError($"Unable to read collected performance data from {parameters.DataFolder}. Please specify another folder or start data collection instead.");
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        internal async Task HandleStartLoginMigration(
            StartLoginMigrationParams parameters,
            RequestContext<LoginMigrationResult> requestContext)
        {
            try
            {
                ILoginsMigrationLogger logger = this.GetLoginsMigrationLogger();
                ILoginsMigration loginMigration = new LoginsMigration(parameters.SourceConnectionString, parameters.TargetConnectionString,
                null, parameters.LoginList, parameters.AADDomainName, logger);

                IDictionary<string, IEnumerable<ReportableException>> exceptionMap = new Dictionary<string, IEnumerable<ReportableException>>();

                exceptionMap.AddExceptions(await loginMigration.StartValidations(CancellationToken.None));
                exceptionMap.AddExceptions(await loginMigration.MigrateLogins(CancellationToken.None));
                exceptionMap.AddExceptions(loginMigration.MigrateServerRoles(CancellationToken.None));
                exceptionMap.AddExceptions(loginMigration.EstablishUserMapping(CancellationToken.None));
                exceptionMap.AddExceptions(await loginMigration.EstablishServerRoleMapping(CancellationToken.None));
                exceptionMap.AddExceptions(loginMigration.SetLoginPermissions(CancellationToken.None));
                exceptionMap.AddExceptions(loginMigration.SetServerRolePermissions(CancellationToken.None));

                LoginMigrationResult results = new LoginMigrationResult()
                {
                    ExceptionMap = exceptionMap
                };

                await requestContext.SendResult(results);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        internal async Task HandleValidateLoginMigration(
            StartLoginMigrationParams parameters,
            RequestContext<LoginMigrationResult> requestContext)
        {
            try
            {
                ILoginsMigrationLogger logger = this.GetLoginsMigrationLogger();
                ILoginsMigration loginMigration = new LoginsMigration(parameters.SourceConnectionString, parameters.TargetConnectionString,
                null, parameters.LoginList, parameters.AADDomainName, logger);

                IDictionary<string, IEnumerable<ReportableException>> exceptionMap = new Dictionary<string, IEnumerable<ReportableException>>();
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                exceptionMap.AddExceptions(await loginMigration.StartValidations(CancellationToken.None));
                stopWatch.Stop();
                TimeSpan elapsedTime = stopWatch.Elapsed;

                LoginMigrationResult results = new LoginMigrationResult()
                {
                    ExceptionMap = exceptionMap,
                    CompletedStep = LoginMigrationStep.StartValidations,
                    ElapsedTime = MigrationServiceHelper.FormatTimeSpan(elapsedTime)

                };

                await requestContext.SendResult(results);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        internal async Task HandleMigrateLogins(
            StartLoginMigrationParams parameters,
            RequestContext<LoginMigrationResult> requestContext)
        {
            try
            {
                ILoginsMigrationLogger logger = this.GetLoginsMigrationLogger();
                ILoginsMigration loginMigration = new LoginsMigration(parameters.SourceConnectionString, parameters.TargetConnectionString,
                null, parameters.LoginList, parameters.AADDomainName, logger);

                IDictionary<string, IEnumerable<ReportableException>> exceptionMap = new Dictionary<string, IEnumerable<ReportableException>>();
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                exceptionMap.AddExceptions(await loginMigration.StartValidations(CancellationToken.None));
                exceptionMap.AddExceptions(await loginMigration.MigrateLogins(CancellationToken.None));
                stopWatch.Stop();
                TimeSpan elapsedTime = stopWatch.Elapsed;

                LoginMigrationResult results = new LoginMigrationResult()
                {
                    ExceptionMap = exceptionMap,
                    CompletedStep = LoginMigrationStep.MigrateLogins,
                    ElapsedTime = MigrationServiceHelper.FormatTimeSpan(elapsedTime)
                };

                await requestContext.SendResult(results);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        internal async Task HandleEstablishUserMapping(
            StartLoginMigrationParams parameters,
            RequestContext<LoginMigrationResult> requestContext)
        {
            try
            {
                ILoginsMigrationLogger logger = this.GetLoginsMigrationLogger();
                ILoginsMigration loginMigration = new LoginsMigration(parameters.SourceConnectionString, parameters.TargetConnectionString,
                null, parameters.LoginList, parameters.AADDomainName, logger);

                IDictionary<string, IEnumerable<ReportableException>> exceptionMap = new Dictionary<string, IEnumerable<ReportableException>>();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                exceptionMap.AddExceptions(await loginMigration.StartValidations(CancellationToken.None));
                exceptionMap.AddExceptions(loginMigration.EstablishUserMapping(CancellationToken.None));
                stopWatch.Stop();
                TimeSpan elapsedTime = stopWatch.Elapsed;

                LoginMigrationResult results = new LoginMigrationResult()
                {
                    ExceptionMap = exceptionMap,
                    CompletedStep = LoginMigrationStep.EstablishUserMapping,
                    ElapsedTime = MigrationServiceHelper.FormatTimeSpan(elapsedTime)
                };

                await requestContext.SendResult(results);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        internal async Task HandleMigrateServerRolesAndSetPermissions(
            StartLoginMigrationParams parameters,
            RequestContext<LoginMigrationResult> requestContext)
        {
            try
            {
                ILoginsMigrationLogger logger = this.GetLoginsMigrationLogger();
                ILoginsMigration loginMigration = new LoginsMigration(parameters.SourceConnectionString, parameters.TargetConnectionString,
                null, parameters.LoginList, parameters.AADDomainName, logger);

                IDictionary<string, IEnumerable<ReportableException>> exceptionMap = new Dictionary<string, IEnumerable<ReportableException>>();
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                exceptionMap.AddExceptions(await loginMigration.StartValidations(CancellationToken.None));
                stopWatch.Stop();
                TimeSpan elapsedTime = stopWatch.Elapsed;

                await this.ServiceHost.SendEvent(
                    LoginMigrationNotification.Type,
                    new LoginMigrationResult()
                    {
                        ExceptionMap = exceptionMap,
                        CompletedStep = LoginMigrationStep.StartValidations,
                        ElapsedTime = MigrationServiceHelper.FormatTimeSpan(elapsedTime)
                    });

                stopWatch.Restart();
                exceptionMap.AddExceptions(loginMigration.MigrateServerRoles(CancellationToken.None));
                stopWatch.Stop();
                elapsedTime = stopWatch.Elapsed;

                await this.ServiceHost.SendEvent(
                    LoginMigrationNotification.Type,
                    new LoginMigrationResult()
                    {
                        ExceptionMap = exceptionMap,
                        CompletedStep = LoginMigrationStep.MigrateServerRoles,
                        ElapsedTime = MigrationServiceHelper.FormatTimeSpan(elapsedTime)
                    });

                stopWatch.Restart();
                exceptionMap.AddExceptions(await loginMigration.EstablishServerRoleMapping(CancellationToken.None));
                stopWatch.Stop();
                elapsedTime = stopWatch.Elapsed;

                await this.ServiceHost.SendEvent(
                    LoginMigrationNotification.Type,
                    new LoginMigrationResult()
                    {
                        ExceptionMap = exceptionMap,
                        CompletedStep = LoginMigrationStep.EstablishServerRoleMapping,
                        ElapsedTime = MigrationServiceHelper.FormatTimeSpan(elapsedTime)
                    });

                stopWatch.Restart();
                exceptionMap.AddExceptions(loginMigration.SetLoginPermissions(CancellationToken.None));
                stopWatch.Stop();
                elapsedTime = stopWatch.Elapsed;

                await this.ServiceHost.SendEvent(
                    LoginMigrationNotification.Type,
                    new LoginMigrationResult()
                    {
                        ExceptionMap = exceptionMap,
                        CompletedStep = LoginMigrationStep.SetLoginPermissions,
                        ElapsedTime = MigrationServiceHelper.FormatTimeSpan(elapsedTime)
                    });

                stopWatch.Restart();
                exceptionMap.AddExceptions(loginMigration.SetServerRolePermissions(CancellationToken.None));
                stopWatch.Stop();
                elapsedTime = stopWatch.Elapsed;

                await this.ServiceHost.SendEvent(
                    LoginMigrationNotification.Type,
                    new LoginMigrationResult()
                    {
                        ExceptionMap = exceptionMap,
                        CompletedStep = LoginMigrationStep.SetServerRolePermissions,
                        ElapsedTime = MigrationServiceHelper.FormatTimeSpan(elapsedTime)
                    });

                LoginMigrationResult results = new LoginMigrationResult()
                {
                    ExceptionMap = exceptionMap,
                    CompletedStep = LoginMigrationStep.SetServerRolePermissions,
                    ElapsedTime = MigrationServiceHelper.FormatTimeSpan(elapsedTime)
                };

                await requestContext.SendResult(results);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        internal RecommendationResultSet GenerateBaselineRecommendations(SqlInstanceRequirements req, GetSkuRecommendationsParams parameters)
        {
            RecommendationResultSet resultSet = new RecommendationResultSet();

            SkuRecommendationServiceProvider provider = new SkuRecommendationServiceProvider(new AzureSqlSkuBillingServiceProvider());
            AzurePreferences prefs = new AzurePreferences()
            {
                EligibleSkuCategories = null,       // eligible SKU list will be adjusted with each recommendation type
                ScalingFactor = parameters.ScalingFactor / 100.0,
                TargetEnvironment = TargetEnvironmentType.Production
            };

            // generate SQL DB recommendations, if applicable
            if (parameters.TargetPlatforms.Contains("AzureSqlDatabase"))
            {
                Stopwatch sqlDbStopwatch = new Stopwatch();
                sqlDbStopwatch.Start();

                prefs.EligibleSkuCategories = GetEligibleSkuCategories("AzureSqlDatabase", parameters.IncludePreviewSkus);
                resultSet.sqlDbResults = provider.GetSkuRecommendation(prefs, req);

                sqlDbStopwatch.Stop();
                resultSet.sqlDbDurationInMs = sqlDbStopwatch.ElapsedMilliseconds;

                SkuRecommendationReport sqlDbReport = new SkuRecommendationReport(
                    new Dictionary<SqlInstanceRequirements, List<SkuRecommendationResult>> { { req, resultSet.sqlDbResults } },
                    AzureSqlTargetPlatform.AzureSqlDatabase.ToString());
                var sqlDbRecommendationReportFileName = String.Format("SkuRecommendationReport-AzureSqlDatabase-Baseline-{0}", DateTime.UtcNow.ToString("yyyyMMddHH-mmss", CultureInfo.InvariantCulture));
                var sqlDbRecommendationReportFullPath = Path.Combine(SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, sqlDbRecommendationReportFileName);
                ExportRecommendationResultsAction.ExportRecommendationResults(sqlDbReport, SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, false, sqlDbRecommendationReportFileName);
                resultSet.sqlDbReportPath = sqlDbRecommendationReportFullPath + ".html";
            }

            // generate SQL MI recommendations, if applicable
            if (parameters.TargetPlatforms.Contains("AzureSqlManagedInstance"))
            {
                Stopwatch sqlMiStopwatch = new Stopwatch();
                sqlMiStopwatch.Start();

                prefs.EligibleSkuCategories = GetEligibleSkuCategories("AzureSqlManagedInstance", parameters.IncludePreviewSkus);
                resultSet.sqlMiResults = provider.GetSkuRecommendation(prefs, req);

                sqlMiStopwatch.Stop();
                resultSet.sqlMiDurationInMs = sqlMiStopwatch.ElapsedMilliseconds;

                SkuRecommendationReport sqlMiReport = new SkuRecommendationReport(
                    new Dictionary<SqlInstanceRequirements, List<SkuRecommendationResult>> { { req, resultSet.sqlMiResults } },
                    AzureSqlTargetPlatform.AzureSqlManagedInstance.ToString());
                var sqlMiRecommendationReportFileName = String.Format("SkuRecommendationReport-AzureSqlManagedInstance-Baseline-{0}", DateTime.UtcNow.ToString("yyyyMMddHH-mmss", CultureInfo.InvariantCulture));
                var sqlMiRecommendationReportFullPath = Path.Combine(SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, sqlMiRecommendationReportFileName);
                ExportRecommendationResultsAction.ExportRecommendationResults(sqlMiReport, SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, false, sqlMiRecommendationReportFileName);
                resultSet.sqlMiReportPath = sqlMiRecommendationReportFullPath + ".html";
            }

            // generate SQL VM recommendations, if applicable
            if (parameters.TargetPlatforms.Contains("AzureSqlVirtualMachine"))
            {
                Stopwatch sqlVmStopwatch = new Stopwatch();
                sqlVmStopwatch.Start();

                prefs.EligibleSkuCategories = GetEligibleSkuCategories("AzureSqlVirtualMachine", parameters.IncludePreviewSkus);
                resultSet.sqlVmResults = provider.GetSkuRecommendation(prefs, req);

                sqlVmStopwatch.Stop();
                resultSet.sqlVmDurationInMs = sqlVmStopwatch.ElapsedMilliseconds;

                SkuRecommendationReport sqlVmReport = new SkuRecommendationReport(
                    new Dictionary<SqlInstanceRequirements, List<SkuRecommendationResult>> { { req, resultSet.sqlVmResults } },
                    AzureSqlTargetPlatform.AzureSqlVirtualMachine.ToString());
                var sqlVmRecommendationReportFileName = String.Format("SkuRecommendationReport-AzureSqlVirtualMachine-Baseline-{0}", DateTime.UtcNow.ToString("yyyyMMddHH-mmss", CultureInfo.InvariantCulture));
                var sqlVmRecommendationReportFullPath = Path.Combine(SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, sqlVmRecommendationReportFileName);
                ExportRecommendationResultsAction.ExportRecommendationResults(sqlVmReport, SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, false, sqlVmRecommendationReportFileName);
                resultSet.sqlVmReportPath = sqlVmRecommendationReportFullPath + ".html";
            }

            return resultSet;
        }

        internal RecommendationResultSet GenerateElasticRecommendations(SqlInstanceRequirements req, GetSkuRecommendationsParams parameters)
        {
            RecommendationResultSet resultSet = new RecommendationResultSet();

            CsvAggregatorForElasticStrategy elasticaggregator = new CsvAggregatorForElasticStrategy(
                instanceId: parameters.TargetSqlInstance,
                directory: parameters.DataFolder,
                queryInterval: parameters.PerfQueryIntervalInSec,
                logger: null,
                dbsToInclude: new HashSet<string>(parameters.DatabaseAllowList));

            // generate SQL DB recommendations, if applicable
            if (parameters.TargetPlatforms.Contains("AzureSqlDatabase"))
            {
                Stopwatch sqlDbStopwatch = new Stopwatch();
                sqlDbStopwatch.Start();

                List<AzureSqlSkuCategory> eligibleSkuCategories = GetEligibleSkuCategories("AzureSqlDatabase", parameters.IncludePreviewSkus);
                ElasticStrategySKURecommendationPipeline pi = new ElasticStrategySKURecommendationPipeline(eligibleSkuCategories);
                DataTable SqlMISpec = pi.SqlMISpec.Copy();
                MISkuRecParams MiSkuRecParams = new MISkuRecParams(pi.SqlGPMIFileSpec, SqlMISpec, elasticaggregator.FileLevelTs, elasticaggregator.InstanceTs, pi.MILookupTable, Convert.ToDouble(parameters.ScalingFactor) / 100.0, parameters.TargetSqlInstance);
                DbSkuRecParams DbSkuRecParams = new DbSkuRecParams(pi.SqlDbSpec, elasticaggregator.DatabaseTs, pi.DbLookupTable, Convert.ToDouble(parameters.ScalingFactor) / 100.0, parameters.TargetSqlInstance);
                resultSet.sqlDbResults = pi.ElasticStrategyGetSkuRecommendation(MiSkuRecParams, DbSkuRecParams, req);

                sqlDbStopwatch.Stop();
                resultSet.sqlDbDurationInMs = sqlDbStopwatch.ElapsedMilliseconds;

                SkuRecommendationReport sqlDbReport = new SkuRecommendationReport(
                    new Dictionary<SqlInstanceRequirements, List<SkuRecommendationResult>> { { req, resultSet.sqlDbResults } },
                    AzureSqlTargetPlatform.AzureSqlDatabase.ToString());
                var sqlDbRecommendationReportFileName = String.Format("SkuRecommendationReport-AzureSqlDatabase-Elastic-{0}", DateTime.UtcNow.ToString("yyyyMMddHH-mmss", CultureInfo.InvariantCulture));
                var sqlDbRecommendationReportFullPath = Path.Combine(SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, sqlDbRecommendationReportFileName);
                ExportRecommendationResultsAction.ExportRecommendationResults(sqlDbReport, SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, false, sqlDbRecommendationReportFileName);
                resultSet.sqlDbReportPath = sqlDbRecommendationReportFullPath + ".html";
            }

            // generate SQL MI recommendations, if applicable
            if (parameters.TargetPlatforms.Contains("AzureSqlManagedInstance"))
            {
                Stopwatch sqlMiStopwatch = new Stopwatch();
                sqlMiStopwatch.Start();

                List<AzureSqlSkuCategory> eligibleSkuCategories = GetEligibleSkuCategories("AzureSqlManagedInstance", parameters.IncludePreviewSkus);
                ElasticStrategySKURecommendationPipeline pi = new ElasticStrategySKURecommendationPipeline(eligibleSkuCategories);
                DataTable SqlMISpec = pi.SqlMISpec.Copy();
                MISkuRecParams MiSkuRecParams = new MISkuRecParams(pi.SqlGPMIFileSpec, SqlMISpec, elasticaggregator.FileLevelTs, elasticaggregator.InstanceTs, pi.MILookupTable, Convert.ToDouble(parameters.ScalingFactor) / 100.0, parameters.TargetSqlInstance);
                DbSkuRecParams DbSkuRecParams = new DbSkuRecParams(pi.SqlDbSpec, elasticaggregator.DatabaseTs, pi.DbLookupTable, Convert.ToDouble(parameters.ScalingFactor) / 100.0, parameters.TargetSqlInstance);
                resultSet.sqlMiResults = pi.ElasticStrategyGetSkuRecommendation(MiSkuRecParams, DbSkuRecParams, req);

                sqlMiStopwatch.Stop();
                resultSet.sqlMiDurationInMs = sqlMiStopwatch.ElapsedMilliseconds;

                SkuRecommendationReport sqlMiReport = new SkuRecommendationReport(
                    new Dictionary<SqlInstanceRequirements, List<SkuRecommendationResult>> { { req, resultSet.sqlMiResults } },
                    AzureSqlTargetPlatform.AzureSqlManagedInstance.ToString());
                var sqlMiRecommendationReportFileName = String.Format("SkuRecommendationReport-AzureSqlManagedInstance-Elastic-{0}", DateTime.UtcNow.ToString("yyyyMMddHH-mmss", CultureInfo.InvariantCulture));
                var sqlMiRecommendationReportFullPath = Path.Combine(SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, sqlMiRecommendationReportFileName);
                ExportRecommendationResultsAction.ExportRecommendationResults(sqlMiReport, SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, false, sqlMiRecommendationReportFileName);
                resultSet.sqlMiReportPath = sqlMiRecommendationReportFullPath + ".html";
            }

            // generate SQL VM recommendations, if applicable
            if (parameters.TargetPlatforms.Contains("AzureSqlVirtualMachine"))
            {
                // elastic model currently doesn't support VM recommendation, return empty list                
                resultSet.sqlVmResults = new List<SkuRecommendationResult> { };
                resultSet.sqlVmDurationInMs = -1;
                resultSet.sqlVmReportPath = String.Empty;
            }

            return resultSet;
        }

        /// <summary>
        /// Handle request to generate provisioning script from a list of SKU recommendations
        /// </summary>
        internal async Task HandleGenerateProvisioningScriptRequest(
            GenerateProvisioningScriptParams parameters,
            RequestContext<GenerateProvisioningScriptResult> requestContext)
        {
            try
            {
                SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath = Path.GetDirectoryName(Logger.LogFileFullPath);

                ProvisioningScriptServiceProvider templateProvider = new ProvisioningScriptServiceProvider();

                string armTemplateFilePath = templateProvider.GenerateAndSaveProvisioningScript(parameters.SkuRecommendations, SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath);

                GenerateProvisioningScriptResult result = new GenerateProvisioningScriptResult{
                    ProvisioningScriptFilePath = armTemplateFilePath
                };

                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        internal class AssessmentRequest : IAssessmentRequest
        {
            private readonly Check[] checks = null;

            public AssessmentRequest(ISqlObjectLocator locator)
            {
                Target = locator ?? throw new ArgumentNullException(nameof(locator));
            }

            public EvaluationContext<object> EvaluationContext { get; }

            public ISqlObjectLocator Target { get; }

            public IEnumerable<Check> Checks
            {
                get
                {
                    return checks;
                }
            }

            public bool TryGetData(string column, out object value)
            {
                return EvaluationContext.TryGetData(column, out value);
            }
        }

        internal async Task<MigrationAssessmentResult> GetAssessmentItems(string[] connectionStrings, string xEventsFilesFolderPath)
        {
            SqlAssessmentConfiguration.EnableLocalLogging = true;
            SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath = Path.GetDirectoryName(Logger.LogFileFullPath);

            SqlConnectionLocator locator = new SqlConnectionLocator();
            locator.ConnectionStrings.AddRange(connectionStrings);
            locator.XeventsFilesFolderPath = xEventsFilesFolderPath;
            DmaEngine engine = new DmaEngine(locator);

            ISqlMigrationAssessmentModel contextualizedAssessmentResult = await engine.GetTargetAssessmentResultsListWithCheck(System.Threading.CancellationToken.None);
            var assessmentReportFileName = String.Format("SqlAssessmentReport-{0}.json", DateTime.UtcNow.ToString("yyyyMMddHH-mmss", CultureInfo.InvariantCulture));
            var assessmentReportFullPath = Path.Combine(SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath, assessmentReportFileName);
            engine.SaveAssessmentResultsToJson(contextualizedAssessmentResult, false, assessmentReportFullPath);

            var server = (contextualizedAssessmentResult.Servers.Count > 0) ? ParseServerAssessmentInfo(contextualizedAssessmentResult.Servers[0], engine) : null;

            return new MigrationAssessmentResult()
            {
                AssessmentResult = server,
                Errors = ParseAssessmentError(contextualizedAssessmentResult.Errors),
                StartTime = contextualizedAssessmentResult.StartedOn.ToString(),
                EndedTime = contextualizedAssessmentResult.EndedOn.ToString(),
                RawAssessmentResult = contextualizedAssessmentResult,
                AssessmentReportPath = assessmentReportFullPath
            };
        }

        internal ServerAssessmentProperties ParseServerAssessmentInfo(IServerAssessmentInfo server, DmaEngine engine)
        {
            return new ServerAssessmentProperties()
            {
                CpuCoreCount = server.Properties.ServerCoreCount,
                PhysicalServerMemory = server.Properties.MaxServerMemoryInUse,
                ServerHostPlatform = server.Properties.ServerHostPlatform,
                ServerVersion = server.Properties.ServerVersion,
                ServerEngineEdition = server.Properties.ServerEngineEdition,
                ServerEdition = server.Properties.ServerEdition,
                IsClustered = server.Properties.IsClustered,
                NumberOfUserDatabases = server.Properties.NumberOfUserDatabases,
                SqlAssessmentStatus = (int)server.Status,
                AssessedDatabaseCount = server.Properties.NumberOfUserDatabases,
                SQLManagedInstanceTargetReadiness = server.TargetReadinesses[Microsoft.SqlServer.DataCollection.Common.Contracts.Advisor.TargetType.AzureSqlManagedInstance],
                Errors = ParseAssessmentError(server.Errors),
                Items = ParseAssessmentResult(server.ServerAssessments, engine),
                Databases = ParseDatabaseAssessmentInfo(server.Databases, engine),
                Name = server.Properties.ServerName
            };
        }

        internal DatabaseAssessmentProperties[] ParseDatabaseAssessmentInfo(IList<IDatabaseAssessmentInfo> databases, DmaEngine engine)
        {
            return databases.Select(d =>
            {
                return new DatabaseAssessmentProperties()
                {
                    Name = d.Properties.Name,
                    CompatibilityLevel = d.Properties.CompatibilityLevel.ToString(),
                    DatabaseSize = d.Properties.SizeMB,
                    IsReplicationEnabled = d.Properties.IsReplicationEnabled,
                    AssessmentTimeInMilliseconds = d.Properties.TSqlScriptAnalysisTimeElapse.TotalMilliseconds,
                    Errors = ParseAssessmentError(d.Errors),
                    Items = ParseAssessmentResult(d.DatabaseAssessments, engine),
                    SQLManagedInstanceTargetReadiness = d.TargetReadinesses[Microsoft.SqlServer.DataCollection.Common.Contracts.Advisor.TargetType.AzureSqlManagedInstance]
                };
            }).ToArray();
        }
        internal ErrorModel[] ParseAssessmentError(IList<Microsoft.SqlServer.DataCollection.Common.Contracts.ErrorHandling.IErrorModel> errors)
        {
            return errors.Select(e =>
            {
                return new ErrorModel()
                {
                    ErrorId = e.ErrorID.ToString(),
                    Message = e.Message,
                    ErrorSummary = e.ErrorSummary,
                    PossibleCauses = e.PossibleCauses,
                    Guidance = e.Guidance,
                };
            }).ToArray();
        }
        internal MigrationAssessmentInfo[] ParseAssessmentResult(IList<ISqlMigrationAssessmentResult> assessmentResults, DmaEngine engine)
        {
            return assessmentResults.Select(r =>
            {
                var check = (Microsoft.SqlServer.Management.Assessment.Checks.Check)r.Check;
                return new MigrationAssessmentInfo()
                {
                    CheckId = check.Id,
                    Description = check.Description,
                    DisplayName = r.Message,
                    HelpLink = check.HelpLink,
                    Level = check.Level.ToString(),
                    TargetType = r.TargetType.ToString(),
                    DatabaseName = r.DatabaseName,
                    ServerName = r.ServerName,
                    Tags = check.Tags.ToArray(),
                    RulesetName = Engine.Configuration.DefaultRuleset.Name,
                    RulesetVersion = Engine.Configuration.DefaultRuleset.Version.ToString(),
                    RuleId = r.FeatureId.ToString(),
                    Message = check.Message,
                    AppliesToMigrationTargetPlatform = r.AppliesToMigrationTargetPlatform.ToString(),
                    IssueCategory = r.IssueCategory.ToString(),
                    ImpactedObjects = ParseImpactedObjects(r.ImpactedObjects),
                    DatabaseRestoreFails = r.DatabaseRestoreFails
                };
            }).ToArray();
        }
        internal ImpactedObjectInfo[] ParseImpactedObjects(IList<Microsoft.SqlServer.DataCollection.Common.Contracts.Advisor.Models.IImpactedObject> impactedObjects)
        {
            return impactedObjects.Select(i =>
            {
                return new ImpactedObjectInfo()
                {
                    Name = i.Name,
                    ImpactDetail = i.ImpactDetail,
                    ObjectType = i.ObjectType
                };
            }).ToArray();
        }

        internal string CreateAssessmentResultKey(ISqlMigrationAssessmentResult assessment)
        {
            return assessment.ServerName + assessment.DatabaseName + assessment.FeatureId.ToString() + assessment.IssueCategory.ToString() + assessment.Message + assessment.TargetType.ToString() + assessment.AppliesToMigrationTargetPlatform.ToString();
        }

        // Returns the list of eligible SKUs to consider, depending on the desired target platform
        internal static List<AzureSqlSkuCategory> GetEligibleSkuCategories(string targetPlatform, bool includePreviewSkus)
        {
            List<AzureSqlSkuCategory> eligibleSkuCategories = new List<AzureSqlSkuCategory>();

            switch (targetPlatform)
            {
                case "AzureSqlDatabase":
                    // Gen5 BC/GP/HS DB
                    eligibleSkuCategories.Add(new AzureSqlSkuPaaSCategory(
                                                    AzureSqlTargetPlatform.AzureSqlDatabase,
                                                    AzureSqlPurchasingModel.vCore,
                                                    AzureSqlPaaSServiceTier.BusinessCritical,
                                                    ComputeTier.Provisioned,
                                                    AzureSqlPaaSHardwareType.Gen5));

                    eligibleSkuCategories.Add(new AzureSqlSkuPaaSCategory(
                                                    AzureSqlTargetPlatform.AzureSqlDatabase,
                                                    AzureSqlPurchasingModel.vCore,
                                                    AzureSqlPaaSServiceTier.GeneralPurpose,
                                                    ComputeTier.Provisioned,
                                                    AzureSqlPaaSHardwareType.Gen5));

                    eligibleSkuCategories.Add(new AzureSqlSkuPaaSCategory(
                                                    AzureSqlTargetPlatform.AzureSqlDatabase,
                                                    AzureSqlPurchasingModel.vCore,
                                                    AzureSqlPaaSServiceTier.HyperScale,
                                                    ComputeTier.Provisioned,
                                                    AzureSqlPaaSHardwareType.Gen5));
                    break;

                case "AzureSqlManagedInstance":
                    // Gen5 BC/GP MI
                    eligibleSkuCategories.Add(new AzureSqlSkuPaaSCategory(
                                                    AzureSqlTargetPlatform.AzureSqlManagedInstance,
                                                    AzureSqlPurchasingModel.vCore,
                                                    AzureSqlPaaSServiceTier.BusinessCritical,
                                                    ComputeTier.Provisioned,
                                                    AzureSqlPaaSHardwareType.Gen5));

                    eligibleSkuCategories.Add(new AzureSqlSkuPaaSCategory(
                                                    AzureSqlTargetPlatform.AzureSqlManagedInstance,
                                                    AzureSqlPurchasingModel.vCore,
                                                    AzureSqlPaaSServiceTier.GeneralPurpose,
                                                    ComputeTier.Provisioned,
                                                    AzureSqlPaaSHardwareType.Gen5));
                    // Premium BC/GP
                    eligibleSkuCategories.Add(new AzureSqlSkuPaaSCategory(
                                                    AzureSqlTargetPlatform.AzureSqlManagedInstance,
                                                    AzureSqlPurchasingModel.vCore,
                                                    AzureSqlPaaSServiceTier.BusinessCritical,
                                                    ComputeTier.Provisioned,
                                                    AzureSqlPaaSHardwareType.PremiumSeries));

                    eligibleSkuCategories.Add(new AzureSqlSkuPaaSCategory(
                                                    AzureSqlTargetPlatform.AzureSqlManagedInstance,
                                                    AzureSqlPurchasingModel.vCore,
                                                    AzureSqlPaaSServiceTier.GeneralPurpose,
                                                    ComputeTier.Provisioned,
                                                    AzureSqlPaaSHardwareType.PremiumSeries));

                        // Premium Memory Optimized BC/GP
                        eligibleSkuCategories.Add(new AzureSqlSkuPaaSCategory(
                                                        AzureSqlTargetPlatform.AzureSqlManagedInstance,
                                                        AzureSqlPurchasingModel.vCore,
                                                        AzureSqlPaaSServiceTier.BusinessCritical,
                                                        ComputeTier.Provisioned,
                                                        AzureSqlPaaSHardwareType.PremiumSeriesMemoryOptimized));

                        eligibleSkuCategories.Add(new AzureSqlSkuPaaSCategory(
                                                        AzureSqlTargetPlatform.AzureSqlManagedInstance,
                                                        AzureSqlPurchasingModel.vCore,
                                                        AzureSqlPaaSServiceTier.GeneralPurpose,
                                                        ComputeTier.Provisioned,
                                                        AzureSqlPaaSHardwareType.PremiumSeriesMemoryOptimized));
                    break;

                case "AzureSqlVirtualMachine":
                    string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                    // load Azure VM capabilities
                    string jsonFile = File.ReadAllText(Path.Combine(assemblyPath, RecommendationConstants.DataFolder, RecommendationConstants.SqlVmCapability));
                    List<AzureSqlIaaSCapability> vmCapabilities = JsonConvert.DeserializeObject<List<AzureSqlIaaSCapability>>(jsonFile);

                    // Eb series capabilities stored separately 
                    string computePreviewFilePath = Path.Combine(assemblyPath, RecommendationConstants.DataFolder, RecommendationConstants.SqlVmPreviewCapability);
                    if (File.Exists(computePreviewFilePath))
                    {
                        jsonFile = File.ReadAllText(computePreviewFilePath);
                        List<AzureSqlIaaSCapability> vmPreviewCapabilities = JsonConvert.DeserializeObject<List<AzureSqlIaaSCapability>>(jsonFile);

                        vmCapabilities.AddRange(vmPreviewCapabilities);
                    }

                    foreach (VirtualMachineFamily family in AzureVirtualMachineFamilyGroup.FamilyGroups[VirtualMachineFamilyType.GeneralPurpose]
                        .Concat(AzureVirtualMachineFamilyGroup.FamilyGroups[VirtualMachineFamilyType.MemoryOptimized]))
                    {
                        var skus = vmCapabilities.Where(c => string.Equals(c.Family, family.ToString(), StringComparison.OrdinalIgnoreCase)).Select(c => c.Name);
                        AzureSqlSkuIaaSCategory category = new AzureSqlSkuIaaSCategory(family);
                        category.AvailableVmSkus.AddRange(skus);

                        eligibleSkuCategories.Add(category);
                    }
                    break;

                default:
                    break;
            }

            return eligibleSkuCategories;
        }

        /// <summary>
        /// Request handler for the certifica migration operation
        /// </summary>
        /// <param name="parameters">Parameters for the operation, as register during the type definition</param>
        /// <param name="requestContext">Context provided by the framework</param>
        /// <returns></returns>
        internal async Task HandleTdeCertificateMigrationRequest(
          CertificateMigrationParams parameters,
          RequestContext<CertificateMigrationResult> requestContext)
        {
            var result = new CertificateMigrationResult();

            var credentials = new StaticTokenCredential(parameters.AccessToken); //New token provided, will change to shared ADS cache later.

            // Reuse the tde migration client
            var tdeMigrationClient = new TdeMigration(
                   parameters.SourceSqlConnectionString,
                   parameters.TargetSubscriptionId,
                   parameters.TargetResourceGroupName,
                   parameters.TargetManagedInstanceName,
                   parameters.NetworkSharePath,
                   parameters.NetworkShareDomain,
                   parameters.NetworkShareUserName,
                   parameters.NetworkSharePassword,
                   credentials
                   );

            foreach (var dbName in parameters.EncryptedDatabases)
            {
                var migrationResult = await MigrateCertificate(tdeMigrationClient, dbName);
                
                var eventData = new CertificateMigrationProgressParams
                {
                    Name = dbName,
                    Success = migrationResult.Success,
                    Message = migrationResult.Message
                };
                await requestContext.SendEvent(CertificateMigrationProgressEvent.Type, eventData);

                result.MigrationStatuses.Add(migrationResult);
            }

            await requestContext.SendResult(result);
        }

        /// <summary>
        /// Individual certificate migration operation
        /// </summary>
        /// <param name="tdeMigrationClient">Instance of the migration client</param>
        /// <param name="dbName">Name of the database to migrate</param>
        /// <returns></returns>
        private async Task<CertificateMigrationEntryResult> MigrateCertificate(TdeMigration tdeMigrationClient, string dbName)
        {
            try
            {
                var result = await tdeMigrationClient.MigrateTdeCertificate(dbName, CancellationToken.None);

                if (result is TdeExceptionResult tdeExceptionResult)
                {
                    return new CertificateMigrationEntryResult { DbName = dbName, Success = result.IsSuccess, Message = tdeExceptionResult.Exception.Message };
                }
                else 
                {
                    return new CertificateMigrationEntryResult { DbName = dbName, Success = result.IsSuccess, Message = result.UserFriendlyMessage };
                }
            }
            catch (Exception ex)
            {
                return new CertificateMigrationEntryResult { DbName = dbName, Success = false, Message = ex.Message };
            }
        }

        private ILoginsMigrationLogger GetLoginsMigrationLogger()
        {
            SqlLoginMigrationConfiguration.AllowTelemetry = true;
            SqlLoginMigrationConfiguration.EnableLocalLogging = true;
            SqlLoginMigrationConfiguration.LogsRootFolderPath = Path.GetDirectoryName(Logger.LogFileFullPath);
            return new DefaultLoginsMigrationLogger();
        }

        /// <summary>
        /// Disposes the Migration Service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                this.DataCollectionController.Dispose();
                disposed = true;
            }
        }
    }
}
