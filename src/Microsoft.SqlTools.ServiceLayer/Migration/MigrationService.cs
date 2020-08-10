//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlServer.Management.Assessment.Checks;
using Microsoft.SqlServer.Management.Assessment.Configuration;
using Microsoft.SqlServer.Management.Assessment.Logics;
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
    public class TestAssessmentResult : IAssessmentResult
    {
        public string Message { get; set; }

        public ICheck Check { get; set;}

        public string TargetPath { get; set; }

        public SqlObjectType TargetType { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }

    public class TestPattern : IPattern<ISqlObjectLocator>
    {
        public bool IsMatch(ISqlObjectLocator value)
        {
            return true;
        }
    }

    public class TestCheck : Check
    {
    }

    public class TestLogic : ILogicsProvider {

        public Task<List<IAssessmentResult>> GetAssessmentResults(
            IAssessmentRequest request, 
            DbConnection connection, 
            EngineConfig configuration)
        {
            
            TestCheck check = new TestCheck()
            {
                Description = "Description",
                DisplayName = "DisplayName",
                Enabled = true,
                HelpLink = "HelpLink",
                Level = SeverityLevel.Information,
                Id = "Is",
                Target = null,
                OriginName = "Description",
                OriginVersion = new Version(1,0),
                Logics = this
            };

            List<IAssessmentResult> results = new List<IAssessmentResult>();
            results.Add(
                new TestAssessmentResult()
                {
                    Message = "Test",
                    Check = check,
                    TargetPath = "Target",
                    TargetType = SqlObjectType.Server,
                    Timestamp = DateTimeOffset.Now
                }
            );
            return Task.FromResult(results);
            
        }
    }

    public class TestRuleset : IRuleset
    {
        public string Name { get; set; }
        public Version Version { get; set; }

        public IEnumerable<ICheck> GetChecks(ISqlObjectLocator target, IEnumerable<ICheck> baseChecks, HashSet<string> ids, HashSet<string> tags)
        {
            List<ICheck> checks = new List<ICheck>();
            checks.Add(new TestCheck()
            {
                Description = "Description",
                DisplayName = "DisplayName",
                Enabled = true,
                HelpLink = "HelpLink",
                Level = SeverityLevel.Critical,
                Id = "Is",
                Target = null,
                OriginName = "Description",
                OriginVersion = new Version(1,0),
                Logics = new TestLogic()
            });
            return checks;
        }

        public void GetSuspects(ISqlObjectLocator target, HashSet<string> ids, HashSet<string> tags)
        {
        }
    }

    /// <summary>
    /// Main class for Migration Service functionality
    /// </summary>
    public sealed class MigrationService : IDisposable
    {        
        private bool disposed;

        private static ConnectionService connectionService = null;
     
        private static readonly Lazy<MigrationService> instance = new Lazy<MigrationService>(() => new MigrationService());
        
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
            RequestContext<MigrationAssessmentsResult> requestContext)
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

                var result = new MigrationAssessmentsResult();
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

                var results = await GetAssessmentItems(server);

            }
            finally
            {
                ConnectionService.Disconnect(new DisconnectParams { OwnerUri = randomUri, Type = null });
            }
        }

        internal async Task<List<CheckInfo>> GetAssessmentItems(SqlObjectLocator target)
        {
            var result = new List<CheckInfo>();

            TestRuleset ruleset = new TestRuleset()
            {
                Name = "Test Ruleset",
                Version = new Version(1,0)
            };
            Engine engine = new Engine();
            engine.Configuration.AddRuleset(ruleset); 
 

            HashSet<string> tags = new HashSet<string>();
            tags.Add("Server");
            List<ICheck> checks = new List<ICheck>();
            checks.Add(new TestCheck()
            {
                Description = "Description",
                DisplayName = "DisplayName",
                Enabled = true,
                HelpLink = "HelpLink",
                Level = SeverityLevel.Information,
                Id = "Is",
                Target = new TestPattern(),
                OriginName = "Description",
                OriginVersion = new Version(1,0),
                Logics = new TestLogic()
            });

            var resultsList = await engine.GetAssessmentResultsList(target, checks);

            foreach (var r in resultsList)
            {
                var targetName = target.Type != SqlObjectType.Server
                                     ? $"{target.ServerName}:{target.Name}"
                                     : target.Name;

                var item = new CheckInfo()
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
                    RulesetVersion = Engine.Configuration.DefaultRuleset.Version.ToString()
                };

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
