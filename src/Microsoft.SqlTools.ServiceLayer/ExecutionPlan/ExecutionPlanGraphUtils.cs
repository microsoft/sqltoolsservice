//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts;
using ExecutionPlanGraph = Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts.ExecutionPlanGraph;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan
{
    public class ExecutionPlanGraphUtils
    {
        public static List<ExecutionPlanGraph> CreateShowPlanGraph(string xml, string fileName)
        {
            ShowPlanGraph[] graphs = ShowPlanGraph.ParseShowPlanXML(xml, ShowPlanType.Unknown);
            return graphs.Select(g => new ExecutionPlanGraph
            {
                Root = ConvertShowPlanTreeToExecutionPlanTree(g.Root),
                Query = g.Statement,
                GraphFile = new ExecutionPlanGraphInfo
                {
                    GraphFileContent = xml,
                    GraphFileType = "xml"
                },
                Recommendations = ParseRecommendations(g, fileName)
            }).ToList();
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

        public static ExecutionPlanEdges ConvertShowPlanEdgeToExecutionPlanEdge(Edge edge)
        {
            return new ExecutionPlanEdges
            {
                RowCount = edge.RowCount,
                RowSize = edge.RowSize,
                Properties = GetProperties(edge.Properties)
            };
        }

        public static List<ExecutionPlanGraphPropertyBase> GetProperties(PropertyDescriptorCollection props)
        {
            List<ExecutionPlanGraphPropertyBase> propsList = new List<ExecutionPlanGraphPropertyBase>();
            foreach (PropertyValue prop in props)
            {
                var complexProperty = prop.Value as ExpandableObjectWrapper;
                if (complexProperty == null)
                {
                    var propertyValue = prop.DisplayValue;
                    propsList.Add(new ExecutionPlanGraphProperty()
                    {
                        Name = prop.DisplayName,
                        Value = propertyValue,
                        ShowInTooltip = prop.ShowInTooltip,
                        DisplayOrder = prop.DisplayOrder,
                        PositionAtBottom = prop.IsLongString,
                        DisplayValue = GetPropertyDisplayValue(prop)
                    });
                }
                else
                {
                    var propertyValue = GetProperties(complexProperty.Properties);
                    propsList.Add(new NestedExecutionPlanGraphProperty()
                    {
                        Name = prop.DisplayName,
                        Value = propertyValue,
                        ShowInTooltip = prop.ShowInTooltip,
                        DisplayOrder = prop.DisplayOrder,
                        PositionAtBottom = prop.IsLongString,
                        DisplayValue = GetPropertyDisplayValue(prop)
                    });
                }

            }
            return propsList;
        }

        private static List<ExecutionPlanRecommendation> ParseRecommendations(ShowPlanGraph g, string fileName)
        {
            return g.Description.MissingIndices.Select(mi => new ExecutionPlanRecommendation
            {
                DisplayString = mi.MissingIndexCaption,
                Query = mi.MissingIndexQueryText,
                QueryWithDescription = ParseMissingIndexQueryText(fileName, mi.MissingIndexImpact, mi.MissingIndexDatabase, mi.MissingIndexQueryText)
            }).ToList();
        }

        /// <summary>
        /// Creates query file text for the recommendations. It has the missing index query along with some lines of description.
        /// </summary>
        /// <param name="fileName">query file name that has generated the plan</param>
        /// <param name="impact">impact of the missing query on performance</param>
        /// <param name="database">database name to create the missing index in</param>
        /// <param name="query">actual query that will be used to create the missing index</param>
        /// <returns></returns>
        private static string ParseMissingIndexQueryText(string fileName, string impact, string database, string query)
        {
            return $@"{SR.MissingIndexDetailsTitle(fileName, impact)}

/*
{string.Format("USE {0}", database)}
GO
{string.Format("{0}", query)}
GO
*/
";
        }

        private static string GetPropertyDisplayValue(PropertyValue property)
        {
            try
            {
                // Get the property value.
                object propertyValue = property.GetValue(property.Value);

                if (propertyValue == null)
                {
                    return String.Empty;
                }

                // Convert the property value to the text.
                return property.Converter.ConvertToString(propertyValue).Trim();
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, e.ToString());
                return String.Empty;
            }
        }

        public static void CopyMatchingNodesIntoSkeletonDTO(SkeletonNodeDTO destRoot, SkeletonNodeDTO srcRoot)
        {
            var srcGraphLookupTable = CreateSkeletonLookupTableFor(srcRoot);

            var queue = new Queue<SkeletonNodeDTO>();
            queue.Enqueue(destRoot);

            while (queue.Count != 0)
            {
                var curNode = queue.Dequeue();

                for (int index = 0; index < curNode.MatchingNodes.Count; ++index)
                {
                    var matchingId = curNode.MatchingNodes[index].BaseNode.ID;
                    var matchingNode = srcGraphLookupTable[matchingId];

                    curNode.MatchingNodes[index] = matchingNode;
                }
                
                foreach (var child in curNode.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        private static Dictionary<int, SkeletonNodeDTO> CreateSkeletonLookupTableFor(SkeletonNodeDTO node)
        {
            var skeletonNodeTable = new Dictionary<int, SkeletonNodeDTO>();
            var queue = new Queue<SkeletonNodeDTO>();
            queue.Enqueue(node);

            while (queue.Count != 0)
            {
                var curNode = queue.Dequeue();

                if (!skeletonNodeTable.ContainsKey(curNode.BaseNode.ID))
                    skeletonNodeTable[curNode.BaseNode.ID] = curNode;
                
                foreach (var child in curNode.Children)
                {
                    queue.Enqueue(child);
                }
            }

            return skeletonNodeTable;
        }
    }
}
