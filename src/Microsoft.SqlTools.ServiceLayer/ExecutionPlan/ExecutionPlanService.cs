//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan.Comparison;
using Microsoft.SqlTools.ServiceLayer.Hosting;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan
{
    /// <summary>
    /// Main class for Execution Plan Service functionality
    /// </summary>
    public sealed class ExecutionPlanService : IDisposable
    {
        private static readonly Lazy<ExecutionPlanService> instance = new Lazy<ExecutionPlanService>(() => new ExecutionPlanService());

        private bool disposed;

        /// <summary>
        /// Reads the in-flight plan of a session. SQL Server fills in the actual row counts so far.
        /// </summary>
        internal const string LiveExecutionPlanQuery =
            "SELECT TOP (1) query_plan FROM sys.dm_exec_query_statistics_xml(@sessionId) WHERE query_plan IS NOT NULL;";

        /// <summary>
        /// Construct a new Execution Plan Service instance with default parameters
        /// </summary>
        private ExecutionPlanService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static ExecutionPlanService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost { get; set; }

        /// <summary>
        /// Initializes the Execution Plan Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            ServiceHost = serviceHost;
            ServiceHost.SetRequestHandler(GetExecutionPlanRequest.Type, HandleGetExecutionPlan, true);
            ServiceHost.SetRequestHandler(ExecutionPlanComparisonRequest.Type, HandleExecutionPlanComparisonRequest, true);
            ServiceHost.SetRequestHandler(GetLiveExecutionPlanRequest.Type, HandleGetLiveExecutionPlan, true);
            ServiceHost.SetRequestHandler(EndLiveExecutionPlanRequest.Type, HandleEndLiveExecutionPlan, true);
        }

        /// <summary>
        /// Reads the in-flight plan of a running query on the editor's monitoring connection, which
        /// opens on the first read and stays open for the next ones.
        /// </summary>
        internal async Task HandleGetLiveExecutionPlan(GetLiveExecutionPlanParams requestParams, RequestContext<GetExecutionPlanResult> requestContext)
        {
            try
            {
                DbConnection connection = await ConnectionService.Instance.GetOrOpenConnection(requestParams.OwnerUri, ConnectionType.LiveQueryStatistics);
                using DbCommand command = connection.CreateCommand();
                command.CommandText = LiveExecutionPlanQuery;
                DbParameter sessionId = command.CreateParameter();
                sessionId.ParameterName = "@sessionId";
                sessionId.DbType = DbType.Int32;
                sessionId.Value = requestParams.SessionId;
                command.Parameters.Add(sessionId);

                // No row means the session isn't running a statement yet
                object plan = await command.ExecuteScalarAsync();
                string planXml;
                if (plan is SqlXml sqlXml)
                {
                    planXml = sqlXml.IsNull ? null : sqlXml.Value;
                }
                else if (plan is XmlReader reader)
                {
                    using (reader)
                    {
                        reader.MoveToContent();
                        planXml = reader.ReadOuterXml();
                    }
                }
                else
                {
                    planXml = Convert.ToString(plan, CultureInfo.InvariantCulture);
                }
                await requestContext.SendResult(new GetExecutionPlanResult
                {
                    Graphs = string.IsNullOrEmpty(planXml)
                        ? new List<ExecutionPlanGraph>()
                        : ExecutionPlanGraphUtils.CreateShowPlanGraph(planXml, "")
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        /// <summary>
        /// Closes the editor's monitoring connection. Only that connection closes; the editor's own
        /// connections and their token refresh are untouched.
        /// </summary>
        internal async Task HandleEndLiveExecutionPlan(EndLiveExecutionPlanParams requestParams, RequestContext<bool> requestContext)
        {
            try
            {
                bool closed = false;
                if (ConnectionService.Instance.TryFindConnection(requestParams.OwnerUri, out ConnectionInfo connectionInfo)
                    && connectionInfo.TryGetConnection(ConnectionType.LiveQueryStatistics, out DbConnection connection))
                {
                    connectionInfo.RemoveConnection(ConnectionType.LiveQueryStatistics);
                    try
                    {
                        connection.Close();
                    }
                    finally
                    {
                        connection.Dispose();
                    }
                    closed = true;
                }
                await requestContext.SendResult(closed);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        private async Task HandleGetExecutionPlan(GetExecutionPlanParams requestParams, RequestContext<GetExecutionPlanResult> requestContext)
        {
            var plans = ExecutionPlanGraphUtils.CreateShowPlanGraph(requestParams.GraphInfo.GraphFileContent, "");
            await requestContext.SendResult(new GetExecutionPlanResult
            {
                Graphs = plans
            });
        }

        /// <summary>
        /// Handles requests for color matching similar nodes.
        /// </summary>
        internal async Task HandleExecutionPlanComparisonRequest(
            ExecutionPlanComparisonParams requestParams,
            RequestContext<ExecutionPlanComparisonResult> requestContext)
        {
            var nodeBuilder = new XmlPlanNodeBuilder(ShowPlanType.Unknown);
            var firstPlanXml = nodeBuilder.GetSingleStatementXml(requestParams.FirstExecutionPlanGraphInfo.GraphFileContent, requestParams.FirstExecutionPlanGraphInfo.PlanIndexInFile);
            var firstGraphSet = ShowPlanGraph.ParseShowPlanXML(firstPlanXml, ShowPlanType.Unknown);
            var firstRootNode = firstGraphSet?[0]?.Root;

            var secondPlanXml = nodeBuilder.GetSingleStatementXml(requestParams.SecondExecutionPlanGraphInfo.GraphFileContent, requestParams.SecondExecutionPlanGraphInfo.PlanIndexInFile);
            var secondGraphSet = ShowPlanGraph.ParseShowPlanXML(secondPlanXml, ShowPlanType.Unknown);
            var secondRootNode = secondGraphSet?[0]?.Root;

            var manager = new SkeletonManager();
            var firstSkeletonNode = manager.CreateSkeleton(firstRootNode);
            var secondSkeletonNode = manager.CreateSkeleton(secondRootNode);
            manager.ColorMatchingSections(firstSkeletonNode, secondSkeletonNode, requestParams.IgnoreDatabaseName);

            var firstGraphComparisonResultDTO = firstSkeletonNode.ConvertToDTO();
            var secondGraphComparisonResultDTO = secondSkeletonNode.ConvertToDTO();
            ExecutionPlanGraphUtils.CopyMatchingNodesIntoSkeletonDTO(firstGraphComparisonResultDTO, secondGraphComparisonResultDTO);
            ExecutionPlanGraphUtils.CopyMatchingNodesIntoSkeletonDTO(secondGraphComparisonResultDTO, firstGraphComparisonResultDTO);

            var result = new ExecutionPlanComparisonResult()
            {
                FirstComparisonResult = firstGraphComparisonResultDTO,
                SecondComparisonResult = secondGraphComparisonResultDTO
            };

            await requestContext.SendResult(result);
        }

        /// <summary>
        /// Disposes the Execution Plan Service
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
