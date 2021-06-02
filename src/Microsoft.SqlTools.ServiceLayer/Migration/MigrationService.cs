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
            this.ServiceHost.SetRequestHandler(ValidateWindowsAccountRequest.Type, HandleValidateWindowsAccountRequest);
            this.ServiceHost.SetRequestHandler(ValidateNetworkFileShareRequest.Type, HandleValidateNetworkFileShareRequest);
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
                var results = await GetAssessmentItems(server, connectionString);
                var result = new MigrationAssessmentResult();
                result.Items.AddRange(results);
                await requestContext.SendResult(result);
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

        internal async Task<List<MigrationAssessmentInfo>> GetAssessmentItems(SqlObjectLocator target, string connectionString)
        {
            SqlAssessmentConfiguration.EnableLocalLogging = true;
            SqlAssessmentConfiguration.EnableReportCreation = true;
            DmaEngine engine = new DmaEngine(connectionString);
            var assessmentResults = await engine.GetTargetAssessmentResultsList();

            var result = new List<MigrationAssessmentInfo>();
            foreach (var r in assessmentResults)
            {
                var migrationResult = r as ISqlMigrationAssessmentResult;
                if (migrationResult == null)
                {
                    continue;
                }

                var targetName = !string.IsNullOrWhiteSpace(migrationResult.DatabaseName)
                                     ? $"{target.ServerName}:{migrationResult.DatabaseName}"
                                     : target.Name;
                var ruleId = migrationResult.FeatureId.ToString();

                var item = new MigrationAssessmentInfo()
                {
                    CheckId = r.Check.Id,
                    Description = r.Check.Description,
                    DisplayName = r.Check.DisplayName,
                    HelpLink = r.Check.HelpLink,
                    Level = r.Check.Level.ToString(),
                    TargetName = targetName,
                    DatabaseName = migrationResult.DatabaseName,
                    ServerName = migrationResult.ServerName,
                    Tags = r.Check.Tags.ToArray(),
                    TargetType = target.Type,
                    RulesetName = Engine.Configuration.DefaultRuleset.Name,
                    RulesetVersion = Engine.Configuration.DefaultRuleset.Version.ToString(),
                    RuleId = ruleId,
                    Message = r.Message,
                    AppliesToMigrationTargetPlatform = migrationResult.AppliesToMigrationTargetPlatform.ToString(),
                    IssueCategory = "Category_Unknown"
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

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
       int dwLogonType, int dwLogonProvider, out SafeAccessTokenHandle phToken);

        internal async Task HandleValidateWindowsAccountRequest(
            ValidateWindowsAccountRequestParams parameters,
            RequestContext<bool> requestContext)
        {
            if (!ValidateWindowsDomainUsername(parameters.Username))
            {
                await requestContext.SendError("Invalid user name format. Example: Domain\\username");
                return;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                int separator = parameters.Username.IndexOf("\\");
                string domainName = parameters.Username.Substring(0, separator);
                string userName = parameters.Username.Substring(separator + 1, parameters.Username.Length - separator - 1);

                const int LOGON32_PROVIDER_DEFAULT = 0;
                const int LOGON32_LOGON_INTERACTIVE = 2;

                SafeAccessTokenHandle safeAccessTokenHandle;
                bool returnValue = LogonUser(userName, domainName, parameters.Password,
                    LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                    out safeAccessTokenHandle);

                if (!returnValue)
                {
                    int ret = Marshal.GetLastWin32Error();
                    string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                    await requestContext.SendError(errorMessage);
                }
                else
                {
                    await requestContext.SendResult(true);
                }
            } 
            else 
            {
                await requestContext.SendResult(true);
            }
        }

        internal async Task HandleValidateNetworkFileShareRequest(
            ValidateNetworkFileShareRequestParams parameters,
            RequestContext<bool> requestContext)
        {
            if (!ValidateWindowsDomainUsername(parameters.Username))
            {
                await requestContext.SendError("Invalid user name format. Example: Domain\\username");
                return;
            }

            if (!ValidateUNCPath(parameters.Path))
            {
                await requestContext.SendError("Invalid network share path. Example: \\\\Servername.domainname.com\\Backupfolder");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                int separator = parameters.Username.IndexOf("\\");
                string domainName = parameters.Username.Substring(0, separator);
                string userName = parameters.Username.Substring(separator + 1, parameters.Username.Length - separator - 1);

                const int LOGON32_PROVIDER_WINNT50 = 3;
                const int LOGON32_LOGON_NEW_CREDENTIALS = 9;

                SafeAccessTokenHandle safeAccessTokenHandle;
                bool returnValue = LogonUser(
                    userName, 
                    domainName, 
                    parameters.Password,
                    LOGON32_LOGON_NEW_CREDENTIALS,
                    LOGON32_PROVIDER_WINNT50,
                    out safeAccessTokenHandle);

                if (!returnValue)
                {
                    int ret = Marshal.GetLastWin32Error();
                    string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                    await requestContext.SendError(errorMessage);
                    return;
                }
                await WindowsIdentity.RunImpersonated(
                safeAccessTokenHandle,
                // User action  
                async () =>
                {
                    if(!Directory.Exists(parameters.Path)){
                        await requestContext.SendError("Cannot connect to file share");
                    } else {
                        await requestContext.SendResult(true);
                    }
                }
                );
            } 
            else 
            {
                await requestContext.SendResult(true);
            }
        }

        /// <summary>
        /// Check if the username is in 'domain\username' format.
        /// </summary>
        /// <returns></returns>
        internal bool ValidateWindowsDomainUsername(string username)
        {
            var domainUserRegex = new Regex(@"^(?<domain>[A-Za-z0-9\._-]*)\\(?<username>[A-Za-z0-9\._-]*)$");
            return domainUserRegex.IsMatch(username);
        }


        /// <summary>
        /// Checks if the file path is in UNC format '\\Servername.domainname.com\Backupfolder'
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal bool ValidateUNCPath(string path)
        {
            return new Uri(path).IsUnc;
        }
    }
}
