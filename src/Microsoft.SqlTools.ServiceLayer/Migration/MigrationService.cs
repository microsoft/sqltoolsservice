//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.SqlServer.DataCollection.Common;
using Microsoft.SqlServer.Management.Assessment.Checks;
using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.Models;
using Microsoft.SqlServer.Migration.Assessment.Common.Engine;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Migration.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Migration.SkuRecommendation.Aggregation;
using Microsoft.SqlServer.Migration.SkuRecommendation.Models.Sql;
using Microsoft.SqlServer.Migration.SkuRecommendation;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Constants;
using Microsoft.SqlServer.Migration.SkuRecommendation.Billing;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models.Sku;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Exceptions;
using Newtonsoft.Json;
using System.Reflection;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models.Environment;

namespace Microsoft.SqlTools.ServiceLayer.Migration
{
    /// <summary>
    /// Main class for Migration Service functionality
    /// </summary>
    public sealed class MigrationService : IDisposable
    {
        private static ConnectionService connectionService = null;

        private static readonly Lazy<MigrationService> instance = new Lazy<MigrationService>(() => new MigrationService());

        private bool disposed;

        /// <summary>
        /// Construct a new MigrationService instance with default parameters
        /// </summary>
        public MigrationService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static MigrationService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionService
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }
            set
            {
                connectionService = value;
            }
        }

        /// <summary>
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

        /// <summary>
        /// Initializes the Migration Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(MigrationAssessmentsRequest.Type, HandleMigrationAssessmentsRequest);
            this.ServiceHost.SetRequestHandler(StartPerfDataCollectionRequest.Type, HandleStartPerfDataCollectionRequest);
            this.ServiceHost.SetRequestHandler(StopPerfDataCollectionRequest.Type, HandleStopPerfDataCollectionRequest);
            this.ServiceHost.SetRequestHandler(RefreshPerfDataCollectionRequest.Type, HandleRefreshPerfDataCollectionRequest);
            this.ServiceHost.SetRequestHandler(GetSkuRecommendationsRequest.Type, HandleGetSkuRecommendationsRequest);
            this.ServiceHost.SetRequestHandler(GenerateProvisioningScriptRequest.Type, HandleGenerateProvisioningScriptRequest);
        }

        /// <summary>
        /// Handle request to start a migration session
        /// </summary>
        internal async Task HandleMigrationAssessmentsRequest(
            MigrationAssessmentsParams parameters,
            RequestContext<MigrationAssessmentResult> requestContext)
        {
            string randomUri = Guid.NewGuid().ToString();
            try
            {
                // get connection
                if (!ConnectionService.TryFindConnection(parameters.OwnerUri, out var connInfo))
                {
                    await requestContext.SendError("Could not find migration connection");
                    return;
                }

                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = randomUri,
                    Connection = connInfo.ConnectionDetails,
                    Type = ConnectionType.Default
                };

                await ConnectionService.Connect(connectParams);

                var connection = await ConnectionService.Instance.GetOrOpenConnection(randomUri, ConnectionType.Default);
                var connectionStrings = new List<string>();
                if (parameters.Databases != null)
                {
                    foreach (string database in parameters.Databases)
                    {
                        connInfo.ConnectionDetails.DatabaseName = database;
                        connectionStrings.Add(ConnectionService.BuildConnectionString(connInfo.ConnectionDetails));
                    }
                    string[] assessmentConnectionStrings = connectionStrings.ToArray();
                    var results = await GetAssessmentItems(assessmentConnectionStrings);
                    await requestContext.SendResult(results);
                }
            }
            finally
            {
                ConnectionService.Disconnect(new DisconnectParams { OwnerUri = randomUri, Type = null });
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
                // get connection
                if (!ConnectionService.TryFindConnection(parameters.OwnerUri, out var connInfo))
                {
                    await requestContext.SendError("Could not find migration connection");
                    return;
                }

                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = randomUri,
                    Connection = connInfo.ConnectionDetails,
                    Type = ConnectionType.Default
                };

                await ConnectionService.Connect(connectParams);
                var connection = await ConnectionService.Instance.GetOrOpenConnection(randomUri, ConnectionType.Default);
                var connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);

                this.DataCollectionController = new SqlDataQueryController(
                    connectionString,
                    parameters.DataFolder,
                    parameters.PerfQueryIntervalInSec,
                    parameters.NumberOfIterations,
                    parameters.StaticQueryIntervalInSec,
                    null);

                this.DataCollectionController.Start();

                // TO-DO: what should be returned?
                await requestContext.SendResult(new StartPerfDataCollectionResult() { DateTimeStarted = DateTime.UtcNow });
            }
            finally
            {
                ConnectionService.Disconnect(new DisconnectParams { OwnerUri = randomUri, Type = null });
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

                SkuRecommendationServiceProvider provider = new SkuRecommendationServiceProvider(new AzureSqlSkuBillingServiceProvider());

                // generate SQL DB recommendations, if applicable
                List<SkuRecommendationResult> sqlDbResults = new List<SkuRecommendationResult>();
                if (parameters.TargetPlatforms.Contains("AzureSqlDatabase"))
                {
                    var prefs = new AzurePreferences()
                    {
                        EligibleSkuCategories = GetEligibleSkuCategories("AzureSqlDatabase", parameters.IncludePreviewSkus),
                        ScalingFactor = parameters.ScalingFactor / 100.0
                    };
                    sqlDbResults = provider.GetSkuRecommendation(prefs, req);

                    if (sqlDbResults.Count < parameters.DatabaseAllowList.Count)
                    {
                        // if there are fewer recommendations than expected, find which databases didn't have a result generated and create a result with a null SKU
                        // TO-DO: in the future the NuGet will supply this logic directly so this won't be necessary anymore
                        List<string> databasesWithRecommendation = sqlDbResults.Select(db => db.DatabaseName).ToList();
                        foreach (var databaseWithoutRecommendation in parameters.DatabaseAllowList.Where(db => !databasesWithRecommendation.Contains(db)))
                        {
                            sqlDbResults.Add(new SkuRecommendationResult()
                            {
                                //SqlInstanceName = sqlDbResults.FirstOrDefault().SqlInstanceName,
                                SqlInstanceName = parameters.TargetSqlInstance,
                                DatabaseName = databaseWithoutRecommendation,
                                TargetSku = null,
                                MonthlyCost = null,
                                Ranking = -1,
                                PositiveJustifications = null,
                                NegativeJustifications = null,
                            });
                        }
                    }
                }

                // generate SQL MI recommendations, if applicable
                List<SkuRecommendationResult> sqlMiResults = new List<SkuRecommendationResult>();
                if (parameters.TargetPlatforms.Contains("AzureSqlManagedInstance"))
                {
                    var prefs = new AzurePreferences()
                    {
                        EligibleSkuCategories = GetEligibleSkuCategories("AzureSqlManagedInstance", parameters.IncludePreviewSkus),
                        ScalingFactor = parameters.ScalingFactor / 100.0
                    };
                    sqlMiResults = provider.GetSkuRecommendation(prefs, req);

                    // if no result was generated, create a result with a null SKU
                    // TO-DO: in the future the NuGet will supply this logic directly so this won't be necessary anymore
                    if (!sqlMiResults.Any())
                    {
                        sqlMiResults.Add(new SkuRecommendationResult()
                        {
                            SqlInstanceName = parameters.TargetSqlInstance,
                            DatabaseName = null,
                            TargetSku = null,
                            MonthlyCost = null,
                            Ranking = -1,
                            PositiveJustifications = null,
                            NegativeJustifications = null,
                        });
                    }
                }

                // generate SQL VM recommendations, if applicable
                List<SkuRecommendationResult> sqlVmResults = new List<SkuRecommendationResult>();
                if (parameters.TargetPlatforms.Contains("AzureSqlVirtualMachine"))
                {
                    var prefs = new AzurePreferences()
                    {
                        EligibleSkuCategories = GetEligibleSkuCategories("AzureSqlVirtualMachine", parameters.IncludePreviewSkus),
                        ScalingFactor = parameters.ScalingFactor / 100.0,
                        TargetEnvironment = TargetEnvironmentType.Production
                    };
                    sqlVmResults = provider.GetSkuRecommendation(prefs, req);

                    // if no result was generated, create a result with a null SKU
                    // TO-DO: in the future the NuGet will supply this logic directly so this won't be necessary anymore
                    if (!sqlVmResults.Any())
                    {
                        sqlVmResults.Add(new SkuRecommendationResult()
                        {
                            SqlInstanceName = parameters.TargetSqlInstance,
                            DatabaseName = null,
                            TargetSku = null,
                            MonthlyCost = null,
                            Ranking = -1,
                            PositiveJustifications = null,
                            NegativeJustifications = null,
                        });
                    }
                }

                GetSkuRecommendationsResult results = new GetSkuRecommendationsResult
                {
                    SqlDbRecommendationResults = sqlDbResults,
                    SqlMiRecommendationResults = sqlMiResults,
                    SqlVmRecommendationResults = sqlVmResults,
                    InstanceRequirements = req
                };

                await requestContext.SendResult(results);
            }
            catch (FailedToQueryCountersException e)
            {
                await requestContext.SendError($"Unable to read collected performance data from {parameters.DataFolder}. Please specify another folder or start data collection instead.");
            }
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

                ArmTemplateServiceProvider templateProvider = new ArmTemplateServiceProvider();
                templateProvider.GenerateAndSaveProvisioningScript(parameters.recs, SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath);
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

        internal async Task<MigrationAssessmentResult> GetAssessmentItems(string[] connectionStrings)
        {
            SqlAssessmentConfiguration.EnableLocalLogging = true;
            SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath = Path.GetDirectoryName(Logger.LogFileFullPath);
            DmaEngine engine = new DmaEngine(connectionStrings);
            ISqlMigrationAssessmentModel contextualizedAssessmentResult = await engine.GetTargetAssessmentResultsListWithCheck(System.Threading.CancellationToken.None);
            engine.SaveAssessmentResultsToJson(contextualizedAssessmentResult, false);
            var server = (contextualizedAssessmentResult.Servers.Count > 0) ? ParseServerAssessmentInfo(contextualizedAssessmentResult.Servers[0], engine) : null;
            return new MigrationAssessmentResult()
            {
                AssessmentResult = server,
                Errors = ParseAssessmentError(contextualizedAssessmentResult.Errors),
                StartTime = contextualizedAssessmentResult.StartedOn.ToString(),
                EndedTime = contextualizedAssessmentResult.EndedOn.ToString(),
                RawAssessmentResult = contextualizedAssessmentResult
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
                    // Gen5 BC/GP DB
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
                    if (includePreviewSkus)
                    {
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
                    }
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
