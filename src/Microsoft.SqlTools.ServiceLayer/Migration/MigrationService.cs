//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlServer.Management.Assessment.Checks;
using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.Models;
using Microsoft.SqlServer.Migration.Assessment.Common.Engine;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Migration.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment;
using Microsoft.Win32.SafeHandles;
using Microsoft.SqlServer.DataCollection.Common;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Security.Principal;
using System.IO;
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
                var serverInfo = ReliableConnectionHelper.GetServerVersion(connection);
                var hostInfo = ReliableConnectionHelper.GetServerHostInfo(connection);

                var server = new SqlObjectLocator
                {
                    Connection = connection,
                    EngineEdition = SqlAssessmentService.GetEngineEdition(serverInfo.EngineEditionId),
                    Name = serverInfo.ServerName,
                    ServerName = serverInfo.ServerName,
                    Type = SqlObjectType.Server,
                    Urn = serverInfo.ServerName,
                    Version = Version.Parse(serverInfo.ServerVersion),
                    Platform = hostInfo.Platform
                };

                var db = SqlAssessmentService.GetDatabaseLocator(server, connection.Database);
                var connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);

                var results = await GetAssessmentItems(connectionString);
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

        internal async Task<MigrationAssessmentResult> GetAssessmentItems(string connectionString)
        {
            SqlAssessmentConfiguration.EnableLocalLogging = true;
            SqlAssessmentConfiguration.EnableReportCreation = true;
            SqlAssessmentConfiguration.AssessmentReportAndLogsRootFolderPath = Path.GetDirectoryName(Logger.LogFileFullPath);
            DmaEngine engine = new DmaEngine(connectionString);
            var assessmentResults = await engine.GetTargetAssessmentResultsList();
            Dictionary<string, ISqlMigrationAssessmentResult> assessmentResultLookup = new Dictionary<string, ISqlMigrationAssessmentResult>();
            foreach (ISqlMigrationAssessmentResult r in assessmentResults)
            {
                assessmentResultLookup.Add(CreateAssessmentResultKey(r as ISqlMigrationAssessmentResult), r as ISqlMigrationAssessmentResult);
            }
            ISqlMigrationAssessmentModel assessmentResultBig = await engine.GetTargetAssessmentResultsList(System.Threading.CancellationToken.None);
            return new MigrationAssessmentResult()
            {
                Result = ParseServerAssessmentInfo(assessmentResultBig.Servers[0], assessmentResultLookup),
                Errors = ParseAssessmentError(assessmentResultBig.Errors).ToArray(),
                StartedOn = assessmentResultBig.StartedOn.ToString(),
                EndedOn = assessmentResultBig.EndedOn.ToString(),
            };
        }

        internal ServerProperties ParseServerAssessmentInfo(IServerAssessmentInfo server,  Dictionary<string, ISqlMigrationAssessmentResult> assessmentResultLookup)
        {
            return new ServerProperties()
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
                Errors = ParseAssessmentError(server.Errors).ToArray(),
                Items = ParseAssessmentResult(server.ServerAssessments, assessmentResultLookup).ToArray(),
                Databases = ParseDatabaseAssessmentInfo(server.Databases, assessmentResultLookup).ToArray(),
                Name = server.Properties.ServerName
            };
        }

        internal List<DatabaseProperties> ParseDatabaseAssessmentInfo(IList<IDatabaseAssessmentInfo> databases, Dictionary<string, ISqlMigrationAssessmentResult> assessmentResultLookup)
        {
            return databases.Select(d =>
            {
                return new DatabaseProperties()
                {
                    Name = d.Properties.Name,
                    CompatibilityLevel = d.Properties.CompatibilityLevel.ToString(),
                    DatabaseSize = d.Properties.SizeMB,
                    IsReplicationEnabled = d.Properties.IsReplicationEnabled,
                    AssessmentTimeInMilliseconds = d.Properties.TSqlScriptAnalysisTimeElapse.TotalMilliseconds,
                    Errors = ParseAssessmentError(d.Errors).ToArray(),
                    Items = ParseAssessmentResult(d.DatabaseAssessments, assessmentResultLookup).ToArray(),
                    SQLManagedInstanceTargetReadiness = d.TargetReadinesses[Microsoft.SqlServer.DataCollection.Common.Contracts.Advisor.TargetType.AzureSqlManagedInstance]
                };
            }).ToList();
        }
        internal List<ErrorModel> ParseAssessmentError(IList<Microsoft.SqlServer.DataCollection.Common.Contracts.ErrorHandling.IErrorModel> errors)
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
            }).ToList();
        }
        internal List<MigrationAssessmentInfo> ParseAssessmentResult(IList<ISqlMigrationAssessmentResult> assessmentResults, Dictionary<string, ISqlMigrationAssessmentResult> assessmentResultLookup)
        {
            return assessmentResults.Select(r =>
            {
                var check = assessmentResultLookup[CreateAssessmentResultKey(r)].Check;
                return new MigrationAssessmentInfo()
                {
                    CheckId = check.Id,
                    Description = check.Description,
                    DisplayName = check.DisplayName,
                    HelpLink = check.HelpLink,
                    Level = check.Level.ToString(),
                    TargetName = r.AppliesToMigrationTargetPlatform.ToString(),
                    DatabaseName = r.DatabaseName,
                    ServerName = r.ServerName,
                    Tags = check.Tags.ToArray(),
                    RulesetName = Engine.Configuration.DefaultRuleset.Name,
                    RulesetVersion = Engine.Configuration.DefaultRuleset.Version.ToString(),
                    RuleId = r.FeatureId.ToString(),
                    Message = r.Message,
                    AppliesToMigrationTargetPlatform = r.AppliesToMigrationTargetPlatform.ToString(),
                    IssueCategory = r.IssueCategory.ToString(),
                    ImpactedObjects = ParseImpactedObjects(r.ImpactedObjects).ToArray()
                };
            }).ToList();
        }
        internal List<ImpactedObjectInfo> ParseImpactedObjects(IList<Microsoft.SqlServer.DataCollection.Common.Contracts.Advisor.Models.IImpactedObject> impactedObjects)
        {
            return impactedObjects.Select(i =>
            {
                return new ImpactedObjectInfo()
                {
                    Name = i.Name,
                    ImpactDetail = i.ImpactDetail,
                    ObjectType = i.ObjectType
                };
            }).ToList();
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
