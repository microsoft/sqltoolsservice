//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    public class ShowPlanGraphUtils
    {
        public static List<ExecutionPlanGraph> CreateShowPlanGraph(string xml, string fileName = "")
        {
            ShowPlanGraph.ShowPlanGraph[] graphs = ShowPlanGraph.ShowPlanGraph.ParseShowPlanXML(xml, ShowPlanGraph.ShowPlanType.Unknown);
            return graphs.Select(g => new ExecutionPlanGraph
            {
                Root = ConvertShowPlanTreeToExecutionPlanTree(g.Root),
                Query = g.Statement,
                RawGraph = xml,
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

        private static ExecutionPlanEdges ConvertShowPlanEdgeToExecutionPlanEdge(Edge edge)
        {
            return new ExecutionPlanEdges
            {
                RowCount = edge.RowCount,
                RowSize = edge.RowSize,
                Properties = GetProperties(edge.Properties)
            };
        }

        private static List<ExecutionPlanGraphPropertyBase> GetProperties(PropertyDescriptorCollection props)
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
                        ShowInTooltip = prop.IsBrowsable,
                        DisplayOrder = prop.DisplayOrder,
                        IsLongString = prop.IsLongString,
                    });
                }
                else
                {
                    var propertyValue = GetProperties(complexProperty.Properties);
                    propsList.Add(new NestedExecutionPlanGraphProperty()
                    {
                        Name = prop.DisplayName,
                        Value = propertyValue,
                        ShowInTooltip = prop.IsBrowsable,
                        DisplayOrder = prop.DisplayOrder,
                        IsLongString = prop.IsLongString,
                    });
                }

            }
            return propsList;
        }

        private static List<ExecutionPlanRecommendation> ParseRecommendations(ShowPlanGraph.ShowPlanGraph g, string fileName)
        {
            if (g.Description != null && g.Description.MissingIndices != null)
            {
                return g.Description.MissingIndices.Select(mi => new ExecutionPlanRecommendation
                {
                    DisplayString = mi.MissingIndexCaption,
                    QueryText = mi.MissingIndexQueryText,
                    FormattedQueryText = ParseMissingIndexQueryText(fileName, mi.MissingIndexImpact, mi.MissingIndexDatabase, mi.MissingIndexQueryText)
                }).ToList();
            }
            return null;
        }

        private static string ParseMissingIndexQueryText(string doctitle, string impact, string database, string query)
        {
            return $@"{string.Format(SR.MissingIndexDetailsTitle, doctitle, impact)}

/*
{string.Format("USE {0}", database)}
GO
{string.Format("{0}", query)}
GO
*/
";
        }
    }
}
