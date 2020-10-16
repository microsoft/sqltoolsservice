//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlServer.Management.Assessment.Configuration;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.Utility;

using AssessmentResultItem = Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts.AssessmentResultItem;
using ConnectionType = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType;
using InvokeParams = Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts.InvokeParams;
using InvokeRequest = Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts.InvokeRequest;

namespace Microsoft.SqlTools.ServiceLayer.SqlAssessment
{
    /// <summary>
    /// Service for running SQL Assessment.
    /// </summary>
    public sealed class SqlAssessmentService : IDisposable
    {
        private const string ApiVersion = "1.0";

        #region Singleton Instance Implementation

        private static readonly Lazy<SqlAssessmentService> LazyInstance
            = new Lazy<SqlAssessmentService>(() => new SqlAssessmentService());

        internal SqlAssessmentService(
            ConnectionService connService,
            WorkspaceService<SqlToolsSettings> workspaceService)
        {
            ConnectionService = connService;
            WorkspaceService = workspaceService;
        }

        private SqlAssessmentService()
        {
            ConnectionService = ConnectionService.Instance;
            WorkspaceService = WorkspaceService<SqlToolsSettings>.Instance;
        }

        /// <summary>
        /// Singleton instance of the query execution service
        /// </summary>
        public static SqlAssessmentService Instance => LazyInstance.Value;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="Engine"/> used to run assessment operations.
        /// </summary>
        internal Engine Engine { get; } = new Engine();

        /// <summary>
        /// Gets the instance of the connection service,
        /// used to get the connection info for a given owner URI.
        /// </summary>
        private ConnectionService ConnectionService { get; }

        private WorkspaceService<SqlToolsSettings> WorkspaceService { get; }

        /// <summary>
        /// Holds a map from the <see cref="Guid"/>
        /// to a <see cref="Task"/> that is being ran.
        /// </summary>
        private readonly Lazy<ConcurrentDictionary<string, Task>> activeRequests =
            new Lazy<ConcurrentDictionary<string, Task>>(() => new ConcurrentDictionary<string, Task>());

        /// <summary>
        /// Gets a map from the <see cref="Guid"/>
        /// to a <see cref="Task"/> that is being ran.
        /// </summary>
        internal ConcurrentDictionary<string, Task> ActiveRequests => activeRequests.Value;

        #endregion

        /// <summary>
        /// Initializes the service with the service host,
        /// registers request handlers and shutdown event handler.
        /// </summary>
        /// <param name="serviceHost">The service host instance to register with</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Register handlers for requests
            serviceHost.SetRequestHandler(InvokeRequest.Type, HandleInvokeRequest);
            serviceHost.SetRequestHandler(GetAssessmentItemsRequest.Type, HandleGetAssessmentItemsRequest);
            serviceHost.SetRequestHandler(GenerateScriptRequest.Type, HandleGenerateScriptRequest);

            // Register handler for shutdown event
            serviceHost.RegisterShutdownTask((shutdownParams, requestContext) =>
            {
                Dispose();
                return Task.FromResult(0);
            });
        }

        #region Request Handlers

        internal Task HandleGetAssessmentItemsRequest(
            GetAssessmentItemsParams itemsParams,
            RequestContext<AssessmentResult<CheckInfo>> requestContext)
        {
            return this.HandleAssessmentRequest(requestContext, itemsParams, this.GetAssessmentItems);
        }

        internal Task HandleInvokeRequest(
            InvokeParams invokeParams,
            RequestContext<AssessmentResult<AssessmentResultItem>> requestContext)
        {
            return this.HandleAssessmentRequest(requestContext, invokeParams, this.InvokeSqlAssessment);
        }

        internal async Task HandleGenerateScriptRequest(
            GenerateScriptParams parameters,
            RequestContext<ResultStatus> requestContext)
        {
            GenerateScriptOperation operation = null;
            try
            {
                operation = new GenerateScriptOperation(parameters);
                TaskMetadata metadata = new TaskMetadata
                {
                    TaskOperation = operation,
                    TaskExecutionMode = parameters.TaskExecutionMode,
                    ServerName = parameters.TargetServerName,
                    DatabaseName = parameters.TargetDatabaseName,
                    Name = SR.SqlAssessmentGenerateScriptTaskName
                };

                var _ = SqlTaskManager.Instance.CreateAndRun<SqlTask>(metadata);

                await requestContext.SendResult(new ResultStatus
                {
                    Success = true,
                    ErrorMessage = operation.ErrorMessage
                });
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, "SQL Assessment: failed to generate a script. Error: " + e);
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                });
            }
        }

        #endregion

        #region Helpers

        private async Task HandleAssessmentRequest<TResult>(
            RequestContext<AssessmentResult<TResult>> requestContext,
            AssessmentParams requestParams,
            Func<SqlObjectLocator, Task<List<TResult>>> assessmentFunction)
            where TResult : AssessmentItemInfo
        {
            try
            {
                string randomUri = Guid.NewGuid().ToString();

                // get connection
                if (!ConnectionService.TryFindConnection(requestParams.OwnerUri, out var connInfo))
                {
                    await requestContext.SendError(SR.SqlAssessmentQueryInvalidOwnerUri);
                    return;
                }

                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = randomUri,
                    Connection = connInfo.ConnectionDetails,
                    Type = ConnectionType.Default
                };

                if(!connInfo.TryGetConnection(ConnectionType.Default, out var connection))
                {
                    await requestContext.SendError(SR.SqlAssessmentConnectingError);
                }

                var workTask = CallAssessmentEngine<TResult>(
                        requestParams,
                        connectParams,
                        randomUri,
                        assessmentFunction)
                    .ContinueWith(async tsk =>
                    {
                        await requestContext.SendResult(tsk.Result);
                    });

                ActiveRequests.TryAdd(randomUri, workTask);
            }
            catch (Exception ex)
            {
                if (ex is StackOverflowException || ex is OutOfMemoryException)
                {
                    throw;
                }

                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// This function obtains a live connection, then calls
        /// an assessment operation specified by <paramref name="assessmentFunc"/>
        /// </summary>
        /// <typeparam name="TResult">
        /// SQL Assessment result item type.
        /// </typeparam>
        /// <param name="requestParams">
        /// Request parameters passed from the host.
        /// </param>
        /// <param name="connectParams">
        /// Connection parameters used to identify and access the target.
        /// </param>
        /// <param name="taskUri">
        /// An URI identifying the request task to enable concurrent execution.
        /// </param>
        /// <param name="assessmentFunc">
        /// A function performing assessment operation for given target.
        /// </param>
        /// <returns>
        /// Returns <see cref="AssessmentResult{TResult}"/> for given target.
        /// </returns>
        internal async Task<AssessmentResult<TResult>> CallAssessmentEngine<TResult>(
            AssessmentParams requestParams,
            ConnectParams connectParams,
            string taskUri,
            Func<SqlObjectLocator, Task<List<TResult>>> assessmentFunc)
            where TResult : AssessmentItemInfo

        {
            var result = new AssessmentResult<TResult>
            {
                ApiVersion = ApiVersion
            };

            await ConnectionService.Connect(connectParams);

            var connection = await ConnectionService.Instance.GetOrOpenConnection(taskUri, ConnectionType.Query);

            try
            {
                var serverInfo = ReliableConnectionHelper.GetServerVersion(connection);
                var hostInfo = ReliableConnectionHelper.GetServerHostInfo(connection);

                var server = new SqlObjectLocator
                {
                    Connection = connection,
                    EngineEdition = GetEngineEdition(serverInfo.EngineEditionId),
                    Name = serverInfo.ServerName,
                    ServerName = serverInfo.ServerName,
                    Type = SqlObjectType.Server,
                    Urn = serverInfo.ServerName,
                    Version = Version.Parse(serverInfo.ServerVersion),
                    Platform = hostInfo.Platform
                };

                switch (requestParams.TargetType)
                {
                    case SqlObjectType.Server:
                        Logger.Write(
                            TraceEventType.Verbose,
                            $"SQL Assessment: running an operation on a server, platform:{server.Platform}, edition:{server.EngineEdition.ToString()}, version:{server.Version}");

                        result.Items.AddRange(await assessmentFunc(server));

                        Logger.Write(
                            TraceEventType.Verbose,
                            $"SQL Assessment: finished an operation on a server, platform:{server.Platform}, edition:{server.EngineEdition.ToString()}, version:{server.Version}");
                        break;
                    case SqlObjectType.Database:
                        var db = GetDatabaseLocator(server, connection.Database);
                        Logger.Write(
                            TraceEventType.Verbose,
                            $"SQL Assessment: running an operation on a database, platform:{server.Platform}, edition:{server.EngineEdition.ToString()}, version:{server.Version}");

                        result.Items.AddRange(await assessmentFunc(db));

                        Logger.Write(
                            TraceEventType.Verbose,
                            $"SQL Assessment: finished an operation on a database, platform:{server.Platform}, edition:{server.EngineEdition.ToString()}, version:{server.Version}");
                        break;
                }

                result.Success = true;
            }
            finally
            {
                ActiveRequests.TryRemove(taskUri, out _);
                ConnectionService.Disconnect(new DisconnectParams { OwnerUri = taskUri, Type = null });
            }

            return result;
        }

        /// <summary>
        /// Invokes SQL Assessment and formats results.
        /// </summary>
        /// <param name="target">
        /// A sequence of target servers or databases to be assessed.
        /// </param>
        /// <returns>
        /// Returns a <see cref="List{AssessmentResultItem}"/>
        /// containing assessment results.
        /// </returns>
        /// <remarks>
        /// Internal for testing
        /// </remarks>
        internal async Task<List<AssessmentResultItem>> InvokeSqlAssessment(SqlObjectLocator target)
        {
            var resultsList = await Engine.GetAssessmentResultsList(target);
            Logger.Write(TraceEventType.Verbose, $"SQL Assessment: got {resultsList.Count} results.");
            
            return resultsList.Select(TranslateAssessmentResult).ToList();
        }

        /// <summary>
        /// Gets the list of checks for given target servers or databases.
        /// </summary>
        /// <param name="target">
        /// A sequence of target servers or databases.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{SqlObjectLocator}"/>
        /// containing checks available for given <paramref name="target"/>.
        /// </returns>
        internal Task<List<CheckInfo>> GetAssessmentItems(SqlObjectLocator target)
        {
            var result = new List<CheckInfo>();

            var resultsList = Engine.GetChecks(target).ToList();
            Logger.Write(TraceEventType.Verbose, $"SQL Assessment: got {resultsList.Count} items.");

            foreach (var r in resultsList)
            {
                var targetName = target.Type != SqlObjectType.Server
                                     ? $"{target.ServerName}:{target.Name}"
                                     : target.Name;

                var item = new CheckInfo()
                               {
                                   CheckId = r.Id,
                                   Description = r.Description,
                                   DisplayName = r.DisplayName,
                                   HelpLink = r.HelpLink,
                                   Level = r.Level.ToString(),
                                   TargetName = targetName,
                                   Tags = r.Tags.ToArray(),
                                   TargetType = target.Type,
                                   RulesetName = Engine.Configuration.DefaultRuleset.Name,
                                   RulesetVersion = Engine.Configuration.DefaultRuleset.Version.ToString()
                               };

                result.Add(item);
            }

            return Task.FromResult(result);
        }

        private AssessmentResultItem TranslateAssessmentResult(IAssessmentResult r)
        {
            var item = new AssessmentResultItem
                           {
                               CheckId = r.Check.Id,
                               Description = r.Check.Description,
                               DisplayName = r.Check.DisplayName,
                               HelpLink = r.Check.HelpLink,
                               Level = r.Check.Level.ToString(),
                               Message = r.Message,
                               TargetName = r.TargetPath,
                               Tags = r.Check.Tags.ToArray(),
                               TargetType = r.TargetType,
                               RulesetVersion = Engine.Configuration.DefaultRuleset.Version.ToString(),
                               RulesetName = Engine.Configuration.DefaultRuleset.Name,
                               Timestamp = r.Timestamp
                           };

            if (r is IAssessmentNote)
            {
                item.Kind = AssessmentResultItemKind.Note;
            }
            else if (r is IAssessmentWarning)
            {
                item.Kind = AssessmentResultItemKind.Warning;
            }
            else if (r is IAssessmentError)
            {
                item.Kind = AssessmentResultItemKind.Error;
            }

            return item;
        }

        /// <summary>
        /// Constructs a <see cref="SqlObjectLocator"/> for specified database.
        /// </summary>
        /// <param name="server">Target server locator.</param>
        /// <param name="databaseName">Target database name.</param>
        /// <returns>Returns a locator for target database.</returns>
        internal static SqlObjectLocator GetDatabaseLocator(SqlObjectLocator server, string databaseName)
        {
            return new SqlObjectLocator
            {
                Connection = server.Connection,
                EngineEdition = server.EngineEdition,
                Name = databaseName,
                Platform = server.Platform,
                ServerName = server.Name,
                Type = SqlObjectType.Database,
                Urn = $"{server.Name}:{databaseName}",
                Version = server.Version
            };
        }

        /// <summary>
        /// Converts numeric <paramref name="representation"/> of engine edition
        /// returned by SERVERPROPERTY('EngineEdition').
        /// </summary>
        /// <param name="representation">
        /// A number returned by SERVERPROPERTY('EngineEdition').
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Engine edition is not supported.</exception>
        /// <returns>
        /// Returns a <see cref="SqlEngineEdition"/>
        /// corresponding to the <paramref name="representation"/>.
        /// </returns>
        internal static SqlEngineEdition GetEngineEdition(int representation)
        {
            switch (representation)
            {
                    case 1: return SqlEngineEdition.PersonalOrDesktopEngine;
                    case 2: return SqlEngineEdition.Standard;
                    case 3: return SqlEngineEdition.Enterprise;
                    case 4: return SqlEngineEdition.Express;
                    case 5: return SqlEngineEdition.AzureDatabase;
                    case 6: return SqlEngineEdition.DataWarehouse;
                    case 7: return SqlEngineEdition.StretchDatabase;
                    case 8: return SqlEngineEdition.ManagedInstance;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(representation),
                            SR.SqlAssessmentUnsuppoertedEdition(representation));
            }
        }

        #endregion

        #region IDisposable Implementation

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var request in ActiveRequests)
                {
                    request.Value.Dispose();
                }

                ActiveRequests.Clear();
            }

            disposed = true;
        }

        ~SqlAssessmentService()
        {
            Dispose(false);
        }

        #endregion
    }
}
