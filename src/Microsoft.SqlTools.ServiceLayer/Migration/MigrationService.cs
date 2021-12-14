//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.DataCollection.Common;
using Microsoft.SqlServer.Management.Assessment.Checks;
using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.Models;
using Microsoft.SqlServer.Migration.Assessment.Common.Engine;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Migration.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models.Sku;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models;
using Microsoft.SqlServer.Migration.SkuRecommendation.Aggregation;
using Microsoft.SqlServer.Migration.SkuRecommendation.Models.Sql;
using Microsoft.SqlServer.Migration.SkuRecommendation.Billing;
using Microsoft.SqlServer.Migration.SkuRecommendation;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Constants;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Text;

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
        /// Initializes the Migration Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(MigrationAssessmentsRequest.Type, HandleMigrationAssessmentsRequest);
            this.ServiceHost.SetRequestHandler(GetSkuRecommendationsRequest.Type, HandleGetSkuRecommendationsRequest);
            this.ServiceHost.SetRequestHandler(StartPerfDataCollectionRequest.Type, HandleStartPerfDataCollectionRequest);
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
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
            finally
            {
                ConnectionService.Disconnect(new DisconnectParams { OwnerUri = randomUri, Type = null });
            }
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
                var results = await GetSkuRecommendationResults(parameters);
                await requestContext.SendResult(results);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        /// <summary>
        /// Handle request to start performance data collection process
        /// </summary>
        internal async Task HandleStartPerfDataCollectionRequest(
            StartPerfDataCollectionParams parameters,
            RequestContext<int> requestContext)
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
                    
                var results = await StartPerfDataCollection(connectionString, parameters);
                await requestContext.SendResult(results);
                
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
            finally
            {
                ConnectionService.Disconnect(new DisconnectParams { OwnerUri = randomUri, Type = null });
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
            var server = (contextualizedAssessmentResult.Servers.Count > 0)? ParseServerAssessmentInfo(contextualizedAssessmentResult.Servers[0], engine): null;
            return new MigrationAssessmentResult()
            {
                AssessmentResult = server,
                Errors = ParseAssessmentError(contextualizedAssessmentResult.Errors),
                StartTime = contextualizedAssessmentResult.StartedOn.ToString(),
                EndedTime = contextualizedAssessmentResult.EndedOn.ToString(),
                RawAssessmentResult = contextualizedAssessmentResult
            };
        }

        internal async Task<GetSkuRecommendationsResult> GetSkuRecommendationResults(GetSkuRecommendationsParams parameters) 
        {
            SqlAssessmentConfiguration.EnableLocalLogging = true;
            SqlAssessmentConfiguration.ReportsAndLogsRootFolderPath = Path.GetDirectoryName(Logger.LogFileFullPath);

            // if DataFolder is not provided, default to reading data from the NuGet default (%localappdata%\Microsoft\SqlAssessment)
            CsvRequirementsAggregator aggregator = new CsvRequirementsAggregator(string.IsNullOrEmpty(parameters.DataFolder) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "SqlAssessment") : parameters.DataFolder);
            
            SqlInstanceRequirements req = aggregator.ComputeSqlInstanceRequirements(null,
                parameters.TargetSqlInstance,
                parameters.TargetPercentile,
                DateTime.ParseExact(parameters.StartTime, RecommendationConstants.TimestampDateTimeFormat, CultureInfo.InvariantCulture),
                DateTime.ParseExact(parameters.EndTime, RecommendationConstants.TimestampDateTimeFormat, CultureInfo.InvariantCulture),
                parameters.PerfQueryIntervalInSec,
                new HashSet<string>(parameters.DatabaseAllowList));

            SkuRecommendationServiceProvider recProvider = new SkuRecommendationServiceProvider(new AzureSqlSkuBillingServiceProvider());

            // only generate recommendations for target platforms specified
            return new GetSkuRecommendationsResult
            {
                SqlDbRecommendationResults = parameters.TargetPlatforms.Contains("AzureSqlDatabase") ? recProvider.GetSkuRecommendation(new AzurePreferences() { EligibleSkuCategories = GetEligibleSkuCategories("AzureSqlDatabase"), ScalingFactor = parameters.ScalingFactor / 100.0 }, req) : new List<SkuRecommendationResult>(),
                SqlMiRecommendationResults = parameters.TargetPlatforms.Contains("AzureSqlManagedInstance") ? recProvider.GetSkuRecommendation(new AzurePreferences() { EligibleSkuCategories = GetEligibleSkuCategories("AzureSqlManagedInstance"), ScalingFactor = parameters.ScalingFactor / 100.0 }, req) : new List<SkuRecommendationResult>(),
                SqlVmRecommendationResults = parameters.TargetPlatforms.Contains("AzureSqlVirtualMachine") ? recProvider.GetSkuRecommendation(new AzurePreferences() { EligibleSkuCategories = GetEligibleSkuCategories("AzureSqlVirtualMachine"), ScalingFactor = parameters.ScalingFactor / 100.0 }, req) : new List<SkuRecommendationResult>()
            };
        }

        // TO-DO: maybe this should be exposed by the NuGet instead?
        // this helper function generates the list of eligible SKUs to consider, depending on the desired target platform
        public static List<AzureSqlSkuCategory> GetEligibleSkuCategories(string targetPlatform)
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
                    break;

                case "AzureSqlVirtualMachine":
                    // Provisioned SQL IaaS
                    eligibleSkuCategories.Add(new AzureSqlSkuIaaSCategory(
                                                    AzureSqlTargetPlatform.AzureSqlVirtualMachine,
                                                    ComputeTier.Provisioned,
                                                    VirtualMachineFamilyType.GeneralPurpose));

                    eligibleSkuCategories.Add(new AzureSqlSkuIaaSCategory(
                                                    AzureSqlTargetPlatform.AzureSqlVirtualMachine,
                                                    ComputeTier.Provisioned,
                                                    VirtualMachineFamilyType.MemoryOptimized));
                    break;

                default:
                    break;
            }

            return eligibleSkuCategories;
        }

        internal async Task<int> StartPerfDataCollection(string connectionString, StartPerfDataCollectionParams parameters)
        {
            const string sqlAssessmentExePath = @"C:\Program Files\Microsoft Data Migration Assistant\SqlAssessmentConsole\SqlAssessment.exe";

            string args = String.Format(
                "PerfDataCollection --outputFolder \"{0}\" --sqlConnectionStrings \"{1}\" --perfQueryIntervalInSec {2} --staticQueryIntervalInSec {3} --numberOfIterations {4}",
                string.IsNullOrEmpty(parameters.DataFolder) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "SqlAssessment") : parameters.DataFolder,    // if DataFolder is not provided, default to writing data to the NuGet default (%localappdata%\Microsoft\SqlAssessment)
                connectionString,
                parameters.PerfQueryIntervalInSec,
                parameters.StaticQueryIntervalInSec,
                parameters.NumberOfIterations);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = sqlAssessmentExePath,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                },
                EnableRaisingEvents = true,
            };

            // TO-DO: if we're committing to data collection being a wrapper for SqlAssessment.exe for now, then add logic to check for an already-running SqlAssessment.exe,
            // find a way to capture error messages, etc.

            proc.Start();
            return proc.Id;
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
                return new MigrationAssessmentInfo()
                {
                    CheckId = r.Check.Id,
                    Description = r.Check.Description,
                    DisplayName = r.Check.DisplayName,
                    HelpLink = r.Check.HelpLink,
                    Level = r.Check.Level.ToString(),
                    TargetType = r.TargetType.ToString(),
                    DatabaseName = r.DatabaseName,
                    ServerName = r.ServerName,
                    Tags = r.Check.Tags.ToArray(),
                    RulesetName = Engine.Configuration.DefaultRuleset.Name,
                    RulesetVersion = Engine.Configuration.DefaultRuleset.Version.ToString(),
                    RuleId = r.FeatureId.ToString(),
                    Message = r.Message,
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
            return assessment.ServerName+assessment.DatabaseName+assessment.FeatureId.ToString()+assessment.IssueCategory.ToString()+assessment.Message + assessment.TargetType.ToString() + assessment.AppliesToMigrationTargetPlatform.ToString();
        }

        /// <summary>
        /// Disposes the Migration Service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}
