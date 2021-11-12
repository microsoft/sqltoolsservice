//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph;
using Microsoft.SqlTools.ServiceLayer.Hosting;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    /// <summary>
    /// Main class for Migration Service functionality
    /// </summary>
    public sealed class ShowPlanService : IDisposable
    {
        private static readonly Lazy<ShowPlanService> instance = new Lazy<ShowPlanService>(() => new ShowPlanService());

        private bool disposed;

        /// <summary>
        /// Construct a new MigrationService instance with default parameters
        /// </summary>
        public ShowPlanService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static ShowPlanService Instance
        {
            get { return instance.Value; }
        }

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
        /// Initializes the ShowPlan Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
        }

        /// <summary>
        /// Disposes the ShowPlan Service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

        public static ExecutionPlanGraph CreateShowPlanGraph(string xml)
        {
            ShowPlanGraph.ShowPlanGraph graph = ShowPlanGraph.ShowPlanGraph.ParseShowPlanXML(xml, ShowPlanGraph.ShowPlanType.Unknown)[0];
            return new ExecutionPlanGraph
            {
                Root = ConvertShowPlanTreeToExecutionPlanTree(graph.Root),
                Query = graph.Statement
            };
        }

        private static ExecutionPlanNode ConvertShowPlanTreeToExecutionPlanTree(Node currentNode)
        {
            return new ExecutionPlanNode
            {
                Type = currentNode.Operation.Image,
                Cost = currentNode.Cost,
                SubTreeCost = currentNode.SubtreeCost,
                Description = currentNode.Description,
                Subtext = currentNode.GetDisplayLinesOfText(),
                RelativeCost = currentNode.RelativeCost,
                Properties = GetProperties(currentNode.Properties),
                Children = currentNode.Children.Select(x => ConvertShowPlanTreeToExecutionPlanTree(x)).ToList(),
                Edges = currentNode.Edges.Select(x => ConvertShowPlanEdgeToExecutionPlanEdge(x)).ToList(),
                Name = currentNode.DisplayName,
                ElapsedTimeInMs = currentNode.ElapsedTimeInMs
            };
        }

        private static ExecutionPlanEdges ConvertShowPlanEdgeToExecutionPlanEdge(Edge edge)
        {
            return new ExecutionPlanEdges
            {
                RowCount = edge.RowCount,
                RowSize = edge.RowSize,
                Properties = GetProperties(edge.Properties)
            };
        }

        private static List<ExecutionPlanGraphElementProperties> GetProperties(PropertyDescriptorCollection props)
        {
            List<ExecutionPlanGraphElementProperties> propsList = new List<ExecutionPlanGraphElementProperties>();
            foreach(PropertyValue prop in props)
            {
                propsList.Add(new ExecutionPlanGraphElementProperties(){
                    Name = prop.DisplayName,
                    FormattedValue = prop.DisplayValue,
                    ShowInTooltip = prop.IsBrowsable,
                    DisplayOrder = prop.DisplayOrder,
                    IsLongString = prop.IsLongString
                });
            }
            return propsList;
        }


    }
}
