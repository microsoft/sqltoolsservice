using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.Connection;
using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer;
using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.ObjectExplorer
{
    public class ObjectExplorerService
    {
        private static readonly Lazy<ObjectExplorerService> _instance
            = new Lazy<ObjectExplorerService>(() => new ObjectExplorerService());
        
        public static ObjectExplorerService Instance => _instance.Value;
        
        private ConnectionService _connectionService;
        private IProtocolEndpoint _serviceHost;
        private readonly ConcurrentDictionary<string, ConnectionCompleteParams> _sessionMap;

        public ObjectExplorerService()
        {
            _sessionMap = new ConcurrentDictionary<string, ConnectionCompleteParams>();
        }
        
        public void InitializeService(IProtocolEndpoint serviceHost, ConnectionService connectionService)
        {
            _serviceHost = serviceHost;
            _connectionService = connectionService;
            
            serviceHost.SetRequestHandler(CreateSessionRequest.Type, HandleCreateSessionRequest);
            serviceHost.SetRequestHandler(ExpandRequest.Type, HandleExpandRequest);
            serviceHost.SetRequestHandler(RefreshRequest.Type, HandleRefreshRequest);
            serviceHost.SetRequestHandler(CloseSessionRequest.Type, HandleCloseSessionRequest);
            serviceHost.SetRequestHandler(FindNodesRequest.Type, HandleFindNodesRequest);
        }

        private async Task HandleCreateSessionRequest(ConnectionDetails connectionDetails, RequestContext<CreateSessionResponse> context)
        {
            Logger.Write(TraceEventType.Verbose, "HandleCreateSessionRequest");
            try
            {
                Parallel.Invoke(async () => await CreateSession(connectionDetails, context));
            }
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
            }
        }

        private async Task CreateSession(ConnectionDetails connectionDetails, RequestContext<CreateSessionResponse> context)
        {
            string ownerUri = BuildOwnerUri(connectionDetails);
            var response = new CreateSessionResponse
            {
                SessionId = ownerUri
            };
            await context.SendResult(response);

            if (!_sessionMap.TryGetValue(ownerUri, out ConnectionCompleteParams session))
            {
                var connectParams = new ConnectParams
                {
                    OwnerUri = ownerUri,
                    Connection = connectionDetails,
                    Type = ConnectionType.ObjectExplorer
                };

                ConnectionCompleteParams resultParams = _connectionService.Connect(connectParams);
                _sessionMap.TryAdd(ownerUri, resultParams);
                session = resultParams;
            }

            var successParams = new SessionCreatedParameters
            {
                SessionId = ownerUri,
                Success = true,
                RootNode = new NodeInfo
                {
                    NodeType = NodeTypes.Server.ToString(),
                    NodePath = "/",
                    Label = $"{session?.ConnectionSummary.ServerName} (Log Analytics {session?.ConnectionSummary.UserName})",
                    IsLeaf = false
                },
            };

            await _serviceHost.SendEvent(CreateSessionCompleteNotification.Type, successParams);
        }

        /// <summary>
        /// Generates an owner URI for the connection based on given details.
        /// </summary>
        /// <param name="connectionDetails">Connection details.</param>
        /// <returns>An owner URI for the connection.</returns>
        private string BuildOwnerUri(ConnectionDetails connectionDetails)
        {
            var keyBuilder = new StringBuilder(connectionDetails.ServerName ?? "NULL");
            
            keyBuilder.Append($"_{connectionDetails.UserName ?? "NULL"}");
            keyBuilder.Append($"_{connectionDetails.AuthenticationType ?? "NULL"}");

            if (!string.IsNullOrEmpty(connectionDetails.DatabaseDisplayName))
            {
                keyBuilder.Append($"_{connectionDetails.DatabaseDisplayName}");
            }

            if (!string.IsNullOrEmpty(connectionDetails.GroupId))
            {
                keyBuilder.Append($"_{connectionDetails.GroupId}");
            }

            return Uri.EscapeUriString(keyBuilder.ToString());
        }

        private async Task HandleExpandRequest(ExpandParams expandParams, RequestContext<bool> context)
        {
            try
            {
                Parallel.Invoke(async () => await ExpandNode(expandParams));
                await context.SendResult(true);
            }
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
            }
        }

        private async Task HandleRefreshRequest(RefreshParams refreshParams, RequestContext<bool> context)
        {
            try
            {
                Parallel.Invoke(async () => await ExpandNode(refreshParams));
                await context.SendResult(true);
            }
            catch(Exception ex)
            {
                await context.SendError(ex.ToString());
            }
        }

        private async Task ExpandNode(ExpandParams expandParams)
        {
            var datasource = _connectionService.GetDataSource(expandParams.SessionId);

            var expandResponse = new ExpandResponse
            {
                SessionId = expandParams.SessionId,
                NodePath = expandParams.NodePath,
                Nodes = datasource.Expand(expandParams.NodePath).ToArray()
            };
            
            await _serviceHost.SendEvent(ExpandCompleteNotification.Type, expandResponse);
        }

        private async Task HandleCloseSessionRequest(CloseSessionParams sessionParams, RequestContext<CloseSessionResponse> context)
        {
            bool success = false;
            Parallel.Invoke(() => success = CloseSession(sessionParams));            

            var closeSessionResponse = new CloseSessionResponse
            {
                SessionId = sessionParams.SessionId,
                Success = success
            };

            await context.SendResult(closeSessionResponse);
        }

        private bool CloseSession(CloseSessionParams sessionParams)
        {
            try
            {
                if (_sessionMap.TryRemove(sessionParams.SessionId, out _))
                {
                    _connectionService.CancelOrDisconnect(sessionParams.SessionId);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private async Task HandleFindNodesRequest(FindNodesParams findNodesParams, RequestContext<FindNodesResponse> context)
        {
            var response = new FindNodesResponse
            {
                Nodes = new List<NodeInfo>()
            };
            
            await context.SendResult(response);
        }
    }
}