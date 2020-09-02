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
using Microsoft.SqlServer.Migration.Assessment.Common.Models;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Migration.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;

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
  
                var results = await GetAssessmentItems(server);
                var result = new MigrationAssessmentResult();
                result.Items.AddRange(results);
                await requestContext.SendResult(result);
            }
            finally
            {
                ConnectionService.Disconnect(new DisconnectParams { OwnerUri = randomUri, Type = null });
            }
        }

        
        internal class AssessmentRequest : IAssessmentRequest
        {
            private readonly Check[] checks;

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

        internal async Task<List<MigrationAssessmentInfo>> GetAssessmentItems(SqlObjectLocator target)
        {
            // var ruleset = DmaEngine.LoadMigrationAssessmentRuleset() as MigrationAssessmentRuleset;
            // var request = new AssessmentRequest(target);
            // List<IAssessmentResult> assessmentResults = new List<IAssessmentResult>();
            // foreach (ICheck check in ruleset.Checks)
            // {
            //    assessmentResults.AddRange(await check.Logics.GetAssessmentResults(request, target.Connection, null));
            // }

            DmaEngine engine = new DmaEngine();
            var assessmentResults = await engine.GetTargetAssessmentResultsList(target);
        
            var result = new List<MigrationAssessmentInfo>();
            foreach (var r in assessmentResults)
            {
                var migrationResult = r as ISqlMigrationAssessmentResult;
                if (migrationResult == null)
                {
                    continue;
                }
                
                var targetName = target.Type != SqlObjectType.Server
                                     ? $"{target.ServerName}:{target.Name}"
                                     : target.Name;

                var item = new MigrationAssessmentInfo()
                {
                    CheckId = r.Check.Id,
                    Description =  r.Check.Description,
                    DisplayName = r.Check.DisplayName,
                    HelpLink = r.Check.HelpLink,
                    Level = r.Check.Level.ToString(),
                    TargetName = targetName,
                    Tags = r.Check.Tags.ToArray(),
                    TargetType = target.Type,
                    RulesetName = Engine.Configuration.DefaultRuleset.Name,
                    RulesetVersion = Engine.Configuration.DefaultRuleset.Version.ToString(),
                    Message = r.Message,
                    AppliesToMigrationTargetPlatform = migrationResult.AppliesToMigrationTargetPlatform.ToString(),
                    IssueCategory = migrationResult.IssueCategory.ToString()
                };

                if (migrationResult.ImpactedObjects != null)
                {
                    ImpactedObjectInfo[] impactedObjects = new ImpactedObjectInfo[migrationResult.ImpactedObjects.Count];
                    for (int i = 0; i < migrationResult.ImpactedObjects.Count; ++i)
                    {
                        var impactedObject = new ImpactedObjectInfo()
                        {
                            Name = migrationResult.ImpactedObjects[i].Name,
                            ImpactDetail = migrationResult.ImpactedObjects[i].ImpactDetail,
                            ObjectType = migrationResult.ImpactedObjects[i].ObjectType
                        };
                        impactedObjects[i] = impactedObject;
                    }
                    item.ImpactedObjects = impactedObjects;
                }

                result.Add(item);
            }
            return result;
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
