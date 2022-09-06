//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    /// <summary>
    /// Execution plan graph object that is sent over JSON RPC
    /// </summary>
    public class ExecutionPlanGraph
    {
        /// <summary>
        /// Root of the execution plan tree
        /// </summary>
        public ExecutionPlanNode Root { get; set; }
        /// <summary>
        /// Underlying query for the execution plan graph
        /// </summary>
        public string Query { get; set; }
        /// <summary>
        /// Graph file that used to generate ExecutionPlanGraph
        /// </summary>
        public ExecutionPlanGraphInfo GraphFile { get; set; }
        /// <summary>
        /// Index recommendations given by show plan to improve query performance
        /// </summary>
        public List<ExecutionPlanRecommendation> Recommendations { get; set; }
    }

    public class ExecutionPlanNode
    {
        /// <summary>
        /// ID for the node.
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// Type of the node. This determines the icon that is displayed for it
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Cost associated with the node
        /// </summary>
        public double Cost { get; set; }
        /// <summary>
        /// Output row count associated with the node
        /// </summary>
        public string RowCountDisplayString { get; set; }
        /// <summary>
        ///  Cost string for the node
        /// </summary>
        /// <value></value>
        public string CostDisplayString { get; set; }
        /// <summary>
        /// Cost of the node subtree
        /// </summary>
        public double SubTreeCost { get; set; }
        /// <summary>
        /// Relative cost of the node compared to its siblings.
        /// </summary>
        public double RelativeCost { get; set; }
        /// <summary>
        /// Time taken by the node operation in milliseconds
        /// </summary>
        public long? ElapsedTimeInMs { get; set; }
        /// <summary>
        /// CPU Time taken by the node operation in milliseconds
        /// </summary>
        public long? ElapsedCpuTimeInMs { get; set; }
        /// <summary>
        /// Node properties to be shown in the tooltip
        /// </summary>
        public List<ExecutionPlanGraphPropertyBase> Properties { get; set; }
        /// <summary>
        /// Display name for the node
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Description associated with the node.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Subtext displayed under the node name
        /// </summary>
        public string[] Subtext { get; set; }
        public List<ExecutionPlanNode> Children { get; set; }
        public List<ExecutionPlanEdges> Edges { get; set; }
        /// <summary>
        /// Add badge icon to nodes like warnings and parallelism
        /// </summary>
        public List<Badge> Badges { get; set; }
        /// <summary>
        /// Top operations table data for the node
        /// </summary>
        public List<TopOperationsDataItem> TopOperationsData { get; set; }
        /// <summary>
        /// Cost metrics for the node
        /// </summary>
        public List<ExecutionPlanCostMetrics> RowReadMetrics { get; set; }
    }

    public class ExecutionPlanGraphPropertyBase
    {
        /// <summary>
        /// Name of the property
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Flag to show/hide props in tooltip
        /// </summary>
        public bool ShowInTooltip { get; set; }
        /// <summary>
        /// Display order of property
        /// </summary>
        public int DisplayOrder { get; set; }
        /// <summary>
        /// Flag to show property at the bottom of tooltip. Generally done for for properties with longer value.
        /// </summary>
        public bool PositionAtBottom { get; set; }
        /// <summary>
        /// Value to be displayed in UI like tooltips and properties View
        /// </summary>
        /// <value></value>
        public string DisplayValue { get; set; }
        /// <summary>
        /// Indicates what kind of value is better amongst 2 values of the same property
        /// </summary>
        public BetterValue BetterValue { get; set; }
        /// <summary>
        /// Indicates the data type of the property
        /// </summary>
        public PropertyValueDataType DataType { get; set; }
    }

    public class ExecutionPlanCostMetrics
    {
        /// <summary>
        /// Name of the cost metric
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Value held by the cost metric
        /// </summary>
        public string? Value { get; set; }
    }

    public class NestedExecutionPlanGraphProperty : ExecutionPlanGraphPropertyBase
    {
        /// <summary>
        /// In case of nested properties, the value field is a list of properties. 
        /// </summary>
        public List<ExecutionPlanGraphPropertyBase> Value { get; set; }
    }

    public class ExecutionPlanGraphProperty : ExecutionPlanGraphPropertyBase
    {
        /// <summary>
        /// Formatted value for the property
        /// </summary>
        public string Value { get; set; }
    }

    public class ExecutionPlanEdges
    {
        /// <summary>
        /// Count of the rows returned by the subtree of the edge.
        /// </summary>
        public double RowCount { get; set; }
        /// <summary>
        /// Size of the rows returned by the subtree of the edge.
        /// </summary>
        public double RowSize { get; set; }
        /// <summary>
        /// Edge properties to be shown in the tooltip.
        /// </summary>
        public List<ExecutionPlanGraphPropertyBase> Properties { get; set; }
    }


    public class ExecutionPlanRecommendation
    {
        /// <summary>
        /// Text displayed in the show plan graph control
        /// </summary>
        public string DisplayString { get; set; }
        /// <summary>
        /// Raw query that is recommended to the user
        /// </summary>
        public string Query { get; set; }
        /// <summary>
        /// Query that will be opened in a new file once the user click on the recommendation
        /// </summary>
        public string QueryWithDescription { get; set; }
    }

    public class ExecutionPlanGraphInfo
    {
        /// <summary>
        /// File contents
        /// </summary>
        public string GraphFileContent { get; set; }
        /// <summary>
        /// File type for execution plan. This will be the file type of the editor when the user opens the graph file
        /// </summary>
        public string GraphFileType { get; set; }
        /// <summary>
        /// Index of the execution plan in the file content
        /// </summary>
        public int PlanIndexInFile { get; set; }
    }

    public class Badge
    {
        /// <summary>
        /// Type of the node overlay. This determines the icon that is displayed for it
        /// </summary>
        public BadgeType Type { get; set; }

        /// <summary>
        /// Text to display for the overlay tooltip
        /// </summary>
        public string Tooltip { get; set; }
    }

    public enum BadgeType
    {
        Warning = 0,
        CriticalWarning = 1,
        Parallelism = 2
    }

    public enum PropertyValueDataType
    {
        Number = 0,
        String = 1,
        Boolean = 2,
        Nested = 3
    }

    public class TopOperationsDataItem
    {
        public string ColumnName { get; set; }
        public PropertyValueDataType DataType { get; set; }
        public object DisplayValue { get; set; }
    }
}