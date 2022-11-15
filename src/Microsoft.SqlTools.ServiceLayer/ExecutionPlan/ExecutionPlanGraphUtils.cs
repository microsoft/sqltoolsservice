//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan
{
    public class ExecutionPlanGraphUtils
    {
        public static List<ExecutionPlanGraph> CreateShowPlanGraph(string xml, string fileName)
        {
            ShowPlanGraph[] graphs = ShowPlanGraph.ParseShowPlanXML(xml, ShowPlan.ShowPlanType.Unknown);
            return graphs.Select((g, index) => new ExecutionPlanGraph
            {
                Root = ConvertShowPlanTreeToExecutionPlanTree(g.Root),
                Query = g.Statement,
                GraphFile = new ExecutionPlanGraphInfo
                {
                    GraphFileContent = xml,
                    GraphFileType = "xml",
                    PlanIndexInFile = index
                },
                Recommendations = ParseRecommendations(g, fileName)
            }).ToList();
        }

        private static void ParseCostMetricProperty(List<CostMetric> costMetrics, string name, PropertyValue? property)
        {
            if (property != null)
            {
                var costMetric = new CostMetric()
                {
                    Name = name,
                    Value = ExecutionPlanGraphUtils.GetPropertyDisplayValue(property)
                };

                costMetrics.Add(costMetric);
            }
        }

        public static ExecutionPlanNode ConvertShowPlanTreeToExecutionPlanTree(Node currentNode)
        {
            var costMetrics = new List<CostMetric>();

            var elapsedCpuTimeInMs = currentNode.ElapsedCpuTimeInMs;
            if (elapsedCpuTimeInMs.HasValue)
            {
                var costMetric = new CostMetric()
                {
                    Name = "ElapsedCpuTime",
                    Value = $"{elapsedCpuTimeInMs.Value}"
                };
                costMetrics.Add(costMetric);
            }

            ExecutionPlanGraphUtils.ParseCostMetricProperty(costMetrics, "EstimateRowsAllExecs", currentNode.Properties["EstimateRowsAllExecs"] as PropertyValue);
            ExecutionPlanGraphUtils.ParseCostMetricProperty(costMetrics, "EstimatedRowsRead", currentNode.Properties["EstimatedRowsRead"] as PropertyValue);
            ExecutionPlanGraphUtils.ParseCostMetricProperty(costMetrics, "ActualRows", currentNode.Properties["ActualRows"] as PropertyValue);
            ExecutionPlanGraphUtils.ParseCostMetricProperty(costMetrics, "ActualRowsRead", currentNode.Properties["ActualRowsRead"] as PropertyValue);

            return new ExecutionPlanNode
            {
                ID = currentNode.ID,
                Type = currentNode.Operation.Image,
                Cost = currentNode.Cost,
                RowCountDisplayString = currentNode.GetRowCountDisplayString(),
                CostDisplayString = currentNode.GetNodeCostDisplayString(),
                SubTreeCost = currentNode.SubtreeCost,
                Description = currentNode.Description,
                Subtext = currentNode.GetDisplayLinesOfText(true),
                RelativeCost = currentNode.RelativeCost,
                Properties = GetProperties(currentNode.Properties),
                Children = currentNode.Children.Select(ConvertShowPlanTreeToExecutionPlanTree).ToList(),
                Edges = currentNode.Edges.Select(ConvertShowPlanEdgeToExecutionPlanEdge).ToList(),
                Badges = GenerateNodeOverlay(currentNode),
                Name = currentNode.DisplayName,
                ElapsedTimeInMs = currentNode.ElapsedTimeInMs,
                TopOperationsData = ParseTopOperationsData(currentNode),
                CostMetrics = costMetrics
            };
        }

        public static List<Badge> GenerateNodeOverlay(Node currentNode)
        {
            List<Badge> overlays = new List<Badge>();

            if (currentNode.HasWarnings)
            {
                if (currentNode.HasCriticalWarnings)
                {
                    overlays.Add(new Badge
                    {
                        Type = BadgeType.CriticalWarning,
                        Tooltip = SR.WarningOverlayTooltip
                    });
                }
                else
                {
                    overlays.Add(new Badge
                    {
                        Type = BadgeType.Warning,
                        Tooltip = SR.WarningOverlayTooltip
                    });
                }
            }
            if (currentNode.IsParallel)
            {
                overlays.Add(new Badge
                {
                    Type = BadgeType.Parallelism,
                    Tooltip = SR.ParallelismOverlayTooltip
                });
            }
            return overlays;
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
                    if (!prop.IsBrowsable)
                    {
                        continue;
                    }
                    var propertyValue = prop.DisplayValue;
                    var propertyDataType = PropertyValueDataType.String;
                    switch (prop.Value)
                    {
                        case string stringValue:
                            propertyDataType = PropertyValueDataType.String;
                            break;
                        case int integerValue:
                        case long longIntegerValue:
                        case uint unsignedIntegerValue:
                        case ulong unsignedLongValue:
                        case float floatValue:
                        case double doubleValue:
                            propertyDataType = PropertyValueDataType.Number;
                            break;
                        case bool booleanValue:
                            propertyDataType = PropertyValueDataType.Boolean;
                            break;
                        default:
                            propertyDataType = PropertyValueDataType.String;
                            break;
                    }

                    propsList.Add(new ExecutionPlanGraphProperty()
                    {
                        Name = prop.DisplayName,
                        Value = propertyValue,
                        ShowInTooltip = prop.ShowInTooltip,
                        DisplayOrder = prop.DisplayOrder,
                        PositionAtBottom = prop.IsLongString,
                        DisplayValue = GetPropertyDisplayValue(prop),
                        BetterValue = prop.BetterValue,
                        DataType = propertyDataType
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
                        DisplayValue = GetPropertyDisplayValue(prop),
                        BetterValue = prop.BetterValue,
                        DataType = PropertyValueDataType.Nested
                    });
                }

            }
            return propsList;
        }

        public static List<TopOperationsDataItem> ParseTopOperationsData(Node currentNode)
        {
            const string OBJECT_COLUMN_KEY = "Object";
            const string ESTIMATED_ROWS_COLUMN_KEY = "EstimateRows";
            const string ACTUAL_ROWS_COLUMN_KEY = "ActualRows";
            const string AVERAGE_ROW_SIZE_COLUMN_KEY = "AvgRowSize";
            const string ACTUAL_EXECUTIONS_COLUMN_KEY = "ActualExecutions";
            const string ESTIMATED_EXECUTIONS_COLUMN_KEY = "EstimateExecutions";
            const string ESTIMATED_CPU_COLUMN_KEY = "EstimateCPU";
            const string ACTUAL_TIME_STATS_KEY = "ActualTimeStatistics";
            const string ACTUAL_CPU_COLUMN_KEY = "ActualCPUms";
            const string ESTIMATED_IO_COLUMN_KEY = "EstimateIO";
            const string PARALLEL_COLUMN_KEY = "Parallel";
            const string ORDERED_COLUMN_KEY = "Ordered";
            const string ACTUAL_REWINDS_COLUMN_KEY = "ActualRewinds";
            const string ESTIMATED_REWINDS_COLUMN_KEY = "EstimateRewinds";
            const string ACTUAL_REBINDS_COLUMN_KEY = "ActualRebinds";
            const string ESTIMATED_REBINDS_COLUMN_KEY = "EstimateRebinds";
            const string PARTITIONED_COLUMN_KEY = "Partitioned";


            List<TopOperationsDataItem> result = new List<TopOperationsDataItem>();
            result.Add(new TopOperationsDataItem
            {
                ColumnName = SR.Operation,
                DataType = PropertyValueDataType.String,
                DisplayValue = currentNode.Operation.DisplayName
            });

            if (currentNode[OBJECT_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.Object,
                    DataType = PropertyValueDataType.String,
                    DisplayValue = ((ExpandableObjectWrapper)currentNode[OBJECT_COLUMN_KEY]).DisplayName
                });
            }

            result.Add(new TopOperationsDataItem
            {
                ColumnName = SR.EstimatedCost,
                DataType = PropertyValueDataType.Number,
                DisplayValue = Math.Round(currentNode.RelativeCost * 100, 2)
            });

            result.Add(new TopOperationsDataItem
            {
                ColumnName = SR.EstimatedSubtree,
                DataType = PropertyValueDataType.Number,
                DisplayValue = Math.Round(currentNode.SubtreeCost, 1)
            });

            if (currentNode[ESTIMATED_ROWS_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.EstimatedRows,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = Math.Round((double)currentNode[ESTIMATED_ROWS_COLUMN_KEY])
                });
            }


            if (currentNode[ACTUAL_ROWS_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.ActualRows,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[ACTUAL_ROWS_COLUMN_KEY]
                });
            }

            if (currentNode[AVERAGE_ROW_SIZE_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.AverageRowSize,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[AVERAGE_ROW_SIZE_COLUMN_KEY]
                });
            }

            if (currentNode[ACTUAL_EXECUTIONS_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.ActualExecutions,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[ACTUAL_EXECUTIONS_COLUMN_KEY]
                });
            }

            if (currentNode[ESTIMATED_EXECUTIONS_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.EstimatedExecutions,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[ESTIMATED_EXECUTIONS_COLUMN_KEY]
                });
            }

            if (currentNode[ESTIMATED_CPU_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.EstimatedCpu,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[ESTIMATED_CPU_COLUMN_KEY]
                });
            }

            if (currentNode[ACTUAL_TIME_STATS_KEY] != null)
            {
                var actualStatsWrapper = currentNode[ACTUAL_TIME_STATS_KEY] as ExpandableObjectWrapper;
                if (actualStatsWrapper != null)
                {
                    var counters = actualStatsWrapper[ACTUAL_CPU_COLUMN_KEY] as RunTimeCounters;
                    if (counters != null)
                    {
                        var elapsedTime = counters.MaxCounter;
                        long ticks = (long)elapsedTime * TimeSpan.TicksPerMillisecond;
                        long time = new DateTime(ticks).Millisecond;
                        result.Add(new TopOperationsDataItem
                        {
                            ColumnName = SR.ActualCpu,
                            DataType = PropertyValueDataType.Number,
                            DisplayValue = time.ToString()
                        });
                    }
                }

            }

            if (currentNode[ESTIMATED_IO_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.EstimatedIO,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[ESTIMATED_IO_COLUMN_KEY]
                });
            }

            if (currentNode[ESTIMATED_ROWS_COLUMN_KEY] != null && currentNode[AVERAGE_ROW_SIZE_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.EstimatedDataSize,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = (double)currentNode[ESTIMATED_ROWS_COLUMN_KEY] * (double)currentNode[AVERAGE_ROW_SIZE_COLUMN_KEY]
                });
            }


            if (currentNode[ACTUAL_ROWS_COLUMN_KEY] != null && currentNode[AVERAGE_ROW_SIZE_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.ActualDataSize,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = (double)(currentNode[ACTUAL_ROWS_COLUMN_KEY] as RunTimeCounters).MaxCounter * (double)currentNode[AVERAGE_ROW_SIZE_COLUMN_KEY]
                });
            }

            if (currentNode[PARALLEL_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.Parallel,
                    DataType = PropertyValueDataType.Boolean,
                    DisplayValue = currentNode[PARALLEL_COLUMN_KEY]
                });
            }

            if (currentNode[ORDERED_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.Ordered,
                    DataType = PropertyValueDataType.Boolean,
                    DisplayValue = currentNode[ORDERED_COLUMN_KEY]
                });
            }

            if (currentNode[ACTUAL_REWINDS_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.ActualRewinds,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[ACTUAL_REWINDS_COLUMN_KEY]
                });
            }

            if (currentNode[ESTIMATED_REWINDS_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.EstimatedRewinds,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[ESTIMATED_REWINDS_COLUMN_KEY]
                });
            }

            if (currentNode[ACTUAL_REBINDS_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.ActualRebinds,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[ACTUAL_REBINDS_COLUMN_KEY]
                });
            }

            if (currentNode[ESTIMATED_REBINDS_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.EstimatedRebinds,
                    DataType = PropertyValueDataType.Number,
                    DisplayValue = currentNode[ESTIMATED_REBINDS_COLUMN_KEY]
                });
            }

            if (currentNode[PARTITIONED_COLUMN_KEY] != null)
            {
                result.Add(new TopOperationsDataItem
                {
                    ColumnName = SR.Partitioned,
                    DataType = PropertyValueDataType.Boolean,
                    DisplayValue = currentNode[PARTITIONED_COLUMN_KEY]
                });
            }
            return result;
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

        private static string GetPropertyDisplayValue(PropertyValue? property)
        {
            if (property == null)
            {
                return string.Empty;
            }

            try
            {
                // Get the property value.
                object propertyValue = property.GetValue(property.Value);

                if (propertyValue == null)
                {
                    return string.Empty;
                }

                // Convert the property value to the text.
                return property.Converter.ConvertToString(propertyValue).Trim();
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, e.ToString());
                return string.Empty;
            }
        }

        public static void CopyMatchingNodesIntoSkeletonDTO(ExecutionGraphComparisonResult destRoot, ExecutionGraphComparisonResult srcRoot)
        {
            var srcGraphLookupTable = srcRoot.CreateSkeletonLookupTable();

            var queue = new Queue<ExecutionGraphComparisonResult>();
            queue.Enqueue(destRoot);

            while (queue.Count != 0)
            {
                var curNode = queue.Dequeue();

                for (int index = 0; index < curNode.MatchingNodesId.Count; ++index)
                {
                    var matchingId = curNode.MatchingNodesId[index];
                    curNode.MatchingNodesId[index] = matchingId;
                }

                foreach (var child in curNode.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }
    }

    public static class ExecutionGraphComparisonResultExtensions
    {
        public static Dictionary<int, ExecutionGraphComparisonResult> CreateSkeletonLookupTable(this ExecutionGraphComparisonResult node)
        {
            var skeletonNodeTable = new Dictionary<int, ExecutionGraphComparisonResult>();
            var queue = new Queue<ExecutionGraphComparisonResult>();
            queue.Enqueue(node);

            while (queue.Count != 0)
            {
                var curNode = queue.Dequeue();

                if (!skeletonNodeTable.ContainsKey(curNode.BaseNode.ID))
                {
                    skeletonNodeTable[curNode.BaseNode.ID] = curNode;
                }

                foreach (var child in curNode.Children)
                {
                    queue.Enqueue(child);
                }
            }

            return skeletonNodeTable;
        }
    }
}
