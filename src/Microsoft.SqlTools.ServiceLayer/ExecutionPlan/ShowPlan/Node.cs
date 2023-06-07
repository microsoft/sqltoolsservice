//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// Status of operator
    /// Based on the query profile DMV view
    /// Pending: when FirstRowTime > 0, Running: FirstRowTime > 0 && CloseTime ==0, Finish: CloseTime > 0
    /// </summary>
    public enum STATUS
    {
        PENDING,
        RUNNING,
        FINISH
    }

    public class Node
    {
        #region Constructor

        public Node(int id, NodeBuilderContext context)
        {
            this.ID = id;
            this.properties = new PropertyDescriptorCollection(new PropertyDescriptor[0]);
            this.children = new List<Node>();
            this.childrenEdges = new List<Edge>();
            this.LogicalOpUnlocName = null;
            this.PhysicalOpUnlocName = null;
            this.root = context.Graph.Root;
            this.root ??= this;
            this.Graph = context.Graph;
        }

        #endregion

        #region Public methods and properties

        public int ID
        {
            get; set;
        }

        public int GroupIndex
        {
            get; set;
        }

        /// <summary>
        /// Gets Node display name
        /// </summary>
        public virtual string DisplayName
        {
            get
            {
                if (this.Operation == Operation.Unknown)
                {
                    return String.Empty;
                }

                // The display name can consist of two lines
                // The first line is the Physical name and the physical kind in parenthesis
                // The second line should contains either Object value or LogicalOp name.
                // The second line should not show the same content as the first line.

                string firstLine = this["PhysicalOp"] as string;
                if (firstLine == null)
                {
                    if (this.Operation == null)
                    {
                        return String.Empty;
                    }

                    firstLine = this.Operation.DisplayName;
                }


                string secondLine;

                object objectValue = this["Object"];
                if (objectValue != null)
                {
                    secondLine = GetObjectNameForDisplay(objectValue);
                }
                else
                {
                    secondLine = this["LogicalOp"] as string;
                    if (secondLine != null)
                    {
                        if (secondLine != firstLine)
                        {
                            // Enclose logical name in parenthesis.
                            secondLine = Constants.Parenthesis(secondLine);
                        }
                        else
                        {
                            // Don't show the second line if its value is the same as on the first line.
                            secondLine = null;
                        }
                    }
                }

                return secondLine == null || secondLine.Length == 0
                    ? firstLine
                    : String.Format(CultureInfo.CurrentCulture, "{0}\n{1}", firstLine, secondLine);
            }
        }

        /// <summary>
        /// Gets Node description
        /// </summary>
        [DisplayOrder(2), DisplayNameDescription(SR.Keys.OperationDescriptionShort, SR.Keys.OperationDescription)]
        public string Description
        {
            get { return this.Operation.Description; }
        }

        /// <summary>
        /// Gets the value that indicates Node parallelism.
        /// </summary>
        public bool IsParallel
        {
            get
            {
                object value = this["Parallel"];
                return value != null ? (bool)value : false;
            }
        }

        /// <summary>
        /// Gets the value that indicates whether the Node has warnings.
        /// </summary>
        [Browsable(false)]
        public bool HasWarnings
        {
            get
            {
                return this["Warnings"] != null;
            }
        }


        /// <summary>
        /// Gets the value that indicates whether the Node has critical warnings.
        /// </summary>
        public bool HasCriticalWarnings
        {
            get
            {
                if (this["Warnings"] != null)
                {
                    ExpandableObjectWrapper wrapper = this["Warnings"] as ExpandableObjectWrapper;
                    if (wrapper["NoJoinPredicate"] != null)
                    {
                        return (bool)wrapper["NoJoinPredicate"];
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Check if this showplan_xml has PDW cost.
        /// </summary>
        private bool HasPDWCost
        {
            get
            {
                return this["PDWAccumulativeCost"] != null;
            }
        }

        /// <summary>
        /// Gets the cost associated with the Node.
        /// </summary>
        [ShowInToolTip, DisplayOrder(8), DisplayNameDescription(SR.Keys.EstimatedOperatorCost, SR.Keys.EstimatedOperatorCostDescription)]
        public string DisplayCost
        {
            get
            {
                double cost = this.RelativeCost * 100;
                if (this.HasPDWCost && cost <= 0)
                {
                    return string.Empty;
                }
                return SR.OperatorDisplayCost(this.Cost, (int)Math.Round(cost));
            }
        }





        /// <summary>
        /// Gets the cost associated with the current Node.
        /// </summary>
        public double Cost
        {
            get
            {
                if (!this.costCalculated)
                {
                    this.cost = this.SubtreeCost;
                    foreach (Node childNode in this.Children)
                    {
                        this.cost -= childNode.SubtreeCost;
                    }

                    // In some cases cost may become a small negative
                    // number due to rounding. Make it 0 in that case.
                    this.cost = Math.Max(this.cost, 0.0);
                    this.costCalculated = true;
                }

                return this.cost;
            }
        }

        /// <summary>
        /// Gets the relative cost associated with the current Node.
        /// </summary>
        public double RelativeCost
        {
            get
            {
                double overallCost = Root.SubtreeCost;
                return overallCost > 0 ? Cost / overallCost : 0;
            }
        }

        /// <summary>
        /// Gets the cost associated with the Node subtree.
        /// </summary>
        [ShowInToolTip, DisplayOrder(9), DisplayNameDescription(SR.Keys.EstimatedSubtreeCost, SR.Keys.EstimatedSubtreeCostDescription)]
        public double SubtreeCost
        {
            get
            {
                if (this.subtreeCost == 0)
                {
                    foreach (Node childNode in this.Children)
                    {
                        this.subtreeCost += childNode.SubtreeCost;
                    }
                }

                return this.subtreeCost;
            }

            set
            {
                this.subtreeCost = value;
            }
        }

        /// <summary>
        /// Max Children X Position.
        /// </summary>
        public int MaxChildrenXPosition;


        /// <summary>
        /// Gets the operation information (localized name, description, image, etc)
        /// </summary>
        public Operation Operation
        {
            get { return this.operation; }
            set { this.operation = value; }
        }

        /// <summary>
        /// Gets node properties.
        /// </summary>
        public PropertyDescriptorCollection Properties
        {
            get { return this.properties; }
        }

        /// <summary>
        /// Gets or sets node property value.
        /// </summary>
        public object this[string propertyName]
        {
            get
            {
                PropertyValue property = this.properties[propertyName] as PropertyValue;
                return property != null ? property.Value : null;
            }

            set
            {
                PropertyValue property = this.properties[propertyName] as PropertyValue;
                if (property != null)
                {
                    // Overwrite existing property value
                    property.Value = value;
                }
                else
                {
                    // Add new property
                    this.properties.Add(PropertyFactory.CreateProperty(propertyName, value));
                }
            }
        }

        public bool IsComputeScalarType()
        {
            return this[NodeBuilderConstants.PhysicalOp] != null
                    && ((string)this[NodeBuilderConstants.PhysicalOp]).StartsWith(SR.Keys.ComputeScalar, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsSeekOrScanType()
        {
            return this[NodeBuilderConstants.PhysicalOp] != null && SeekOrScanPhysicalOpList.Contains(this.PhysicalOpUnlocName);
        }

        /// <summary>
        /// Gets collection of node children.
        /// </summary>
        public List<Node> Children
        {
            get { return this.children; }
        }

        /// <summary>
        /// Gets collection of node children.
        /// </summary>
        public List<Edge> Edges
        {
            get { return this.childrenEdges; }
        }


        /// <summary>
        /// Gets current node parent.
        /// </summary>
        public Node Parent
        {
            get;
        }

        public Node Root
        {
            get { return this.root; }
        }

        public ShowPlanGraph Graph
        {
            get => this.graph;
            set
            {
                this.graph = value;
            }
        }

        /// <summary>
        /// Identifies if this node is finished executing
        /// </summary>
        /// <returns>True if finished</returns>
        public bool IsFinished()
        {
            var statusObject = this[NodeBuilderConstants.Status] as STATUS?;

            return statusObject != null && (STATUS)statusObject == STATUS.FINISH;
        }

        /// <summary>
        /// Identifies if this node is executing
        /// </summary>
        /// <returns>True if running</returns>
        public bool IsRunning()
        {
            var statusObject = this[NodeBuilderConstants.Status] as STATUS?;

            return statusObject != null && (STATUS)statusObject == STATUS.RUNNING;
        }

        /// <summary>
        /// Returns whether the properties of two nodes are logically similar enough to be considered
        /// the same for skeleton comparison purposes
        /// Does not check children
        /// </summary>
        /// <param name="nodeToCompare"></param>
        /// <param name="ignoreDatabaseName"></param>
        /// <returns></returns>
        ///
        public bool IsLogicallyEquivalentTo(Node nodeToCompare, bool ignoreDatabaseName)
        {
            // same exact node
            if (this == nodeToCompare)
                return true;

            // seek and scan types are equivalent so ignore them when comparing logical op
            if (this[NodeBuilderConstants.LogicalOp] != nodeToCompare[NodeBuilderConstants.LogicalOp] &&
                (!this.IsSeekOrScanType() || !nodeToCompare.IsSeekOrScanType()))
                return false;

            // one has object but other does not
            if (this[objectProperty] != null && nodeToCompare[objectProperty] == null || nodeToCompare[objectProperty] != null && this[objectProperty] == null)
                return false;

            // both have object
            if (this[objectProperty] != null && nodeToCompare[objectProperty] != null)
            {
                ExpandableObjectWrapper objectProp1 = (ExpandableObjectWrapper)this[objectProperty];
                ExpandableObjectWrapper objectProp2 = (ExpandableObjectWrapper)nodeToCompare[objectProperty];
                // object property doesn't match
                // by default, we ignore DB name
                // for ex: "[master].[sys].[sysobjvalues].[clst] [e]" and "[master_copy].[sys].[sysobjvalues].[clst] [e]" would be same
                if (ignoreDatabaseName)
                {
                    if (!CompareObjectPropertyValue((PropertyValue)(objectProp1.Properties[SR.ObjectServer]), (PropertyValue)(objectProp2.Properties[SR.ObjectServer])))
                    {
                        return false;
                    }
                    if (!CompareObjectPropertyValue((PropertyValue)(objectProp1.Properties[SR.ObjectSchema]), (PropertyValue)(objectProp2.Properties[SR.ObjectSchema])))
                    {
                        return false;
                    }
                    if (!CompareObjectPropertyValue((PropertyValue)(objectProp1.Properties[SR.ObjectTable]), (PropertyValue)(objectProp2.Properties[SR.ObjectTable])))
                    {
                        return false;
                    }
                    if (!CompareObjectPropertyValue((PropertyValue)(objectProp1.Properties[SR.ObjectAlias]), (PropertyValue)(objectProp2.Properties[SR.ObjectAlias])))
                    {
                        return false;
                    }

                    // check for CloneAccessScope if it is specified
                    PropertyValue specified1 = (PropertyValue)(objectProp1.Properties["CloneAccessScopeSpecified"]);
                    PropertyValue specified2 = (PropertyValue)(objectProp2.Properties["CloneAccessScopeSpecified"]);
                    if (specified1 == null && specified2 != null || specified1 != null && specified2 == null)
                    {
                        return false;
                    }
                    else if (specified1 != null && specified2 != null)
                    {
                        if ((bool)(specified1.Value) != (bool)(specified2.Value))
                        {
                            return false;
                        }
                        else
                        {
                            if ((bool)(specified1.Value) == true)
                            {
                                PropertyValue p1 = (PropertyValue)(objectProp1.Properties["CloneAccessScope"]);
                                PropertyValue p2 = (PropertyValue)(objectProp2.Properties["CloneAccessScope"]);
                                if (p1 == null && p2 != null || p1 != null && p2 == null)
                                {
                                    return false;
                                }
                                else if (p1 != null && p2 != null)
                                {
                                    if ((CloneAccessScopeType)(p1.Value) != (CloneAccessScopeType)(p2.Value))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (objectProp1.DisplayName != objectProp2.DisplayName)
                    {
                        return false;
                    }
                }
            }
            // same logical op, no other criteria
            return true;
        }

        public long? ElapsedTimeInMs
        {
            get
            {
                long? time = null;
                var actualStatsWrapper = this["ActualTimeStatistics"] as ExpandableObjectWrapper;
                if (actualStatsWrapper != null)
                {
                    var counters = actualStatsWrapper["ActualElapsedms"] as RunTimeCounters;
                    if (counters != null)
                    {
                        var elapsedTime = counters.MaxCounter;
                        long ticks = (long)elapsedTime * TimeSpan.TicksPerMillisecond;
                        time = new DateTime(ticks).Millisecond;
                    }
                }
                return time;
            }
        }

        public long? ElapsedCpuTimeInMs
        {
            get
            {
                long? time = null;
                var actualStatsWrapper = this["ActualTimeStatistics"] as ExpandableObjectWrapper;
                if (actualStatsWrapper != null)
                {
                    var counters = actualStatsWrapper["ActualCPUms"] as RunTimeCounters;
                    if (counters != null)
                    {
                        var elapsedTime = counters.MaxCounter;
                        long ticks = (long)elapsedTime * TimeSpan.TicksPerMillisecond;
                        time = new DateTime(ticks).Millisecond;
                    }
                }
                return time;
            }
        }

        /// <summary>
        /// ENU name for Logical Operator
        /// </summary>
        public string LogicalOpUnlocName { get; set; }

        /// <summary>
        /// ENU name for Physical Operator
        /// </summary>
        public string PhysicalOpUnlocName { get; set; }

        #endregion

        #region Implementation details

        /// <summary>
        /// Gets short object name for display.
        /// Since database and schema is not important and displaying table first is much useful,
        /// we are displaying object name in [Table].[Index] [Alias] format.
        /// </summary>
        /// <param name="objectProperty">Object property in the property bag</param>
        private string GetObjectNameForDisplay(object objectProperty)
        {
            string objectNameForDisplay = string.Empty;

            Debug.Assert(objectProperty != null);
            if (objectProperty != null)
            {
                objectNameForDisplay = objectProperty.ToString();

                ExpandableObjectWrapper objectWrapper = objectProperty as ExpandableObjectWrapper;
                Debug.Assert(objectWrapper != null);
                if (objectWrapper != null)
                {
                    objectNameForDisplay = ObjectWrapperTypeConverter.MergeString(".", objectWrapper["Table"], objectWrapper["Index"]);
                    objectNameForDisplay = ObjectWrapperTypeConverter.MergeString(" ", objectNameForDisplay, objectWrapper["Alias"]);
                }
            }

            return objectNameForDisplay;
        }

        /// <summary>
        /// used to compare multiple string type PropertyValue in Object properties,
        /// for ex: Server, Database, Schema, Table, Index, etc...
        /// </summary>
        /// <returns>True if two PropertyValue are equal</returns>
        private bool CompareObjectPropertyValue(PropertyValue p1, PropertyValue p2)
        {
            if (p1 == null && p2 != null || p1 != null && p2 == null)
            {
                return false;
            }
            else if (p1 != null && p2 != null)
            {
                string s1 = p1.Value as string;
                string s2 = p2.Value as string;
                if (string.Compare(s1, s2, StringComparison.Ordinal) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets lines of text displayed under the icon.
        /// </summary>
        /// <param name="roundCostForSmallGraph">Converts decimal costs in case of graph with less than 20 nodes.</param>
        /// <returns>Array of strings.</returns>
        public string[] GetDisplayLinesOfText(bool roundCostForSmallGraph = false)
        {
            string newDisplayNameLines = this.DisplayName;

            return newDisplayNameLines.Split('\n');
        }

        /// <summary>
        /// Provide a string for the actual elapsed time if it is available
        /// </summary>
        /// <returns>formatted string of execution time</returns>
        public string GetElapsedTimeDisplayString()
        {
            string formattedTime = null;

            var actualStatsWrapper = this["ActualTimeStatistics"] as ExpandableObjectWrapper;
            if (actualStatsWrapper != null)
            {
                var counters = actualStatsWrapper["ActualElapsedms"] as RunTimeCounters;
                if (counters != null)
                {
                    var elapsedTime = counters.MaxCounter;
                    long ticks = (long)elapsedTime * TimeSpan.TicksPerMillisecond;
                    var time = new DateTime(ticks);
                    if (ticks < 1000L * TimeSpan.TicksPerMillisecond * 60) // 60 seconds
                    {
                        formattedTime = time.ToString("s.fff") + "s";
                    }
                    else
                    {
                        // calculate the hours
                        long hours = ticks / (1000L * TimeSpan.TicksPerMillisecond * 60 * 60); //1 hour
                        formattedTime = hours.ToString() + time.ToString(":mm:ss");
                    }
                }
            }

            return formattedTime;
        }

        /// <summary>
        /// Provide a string for the actual rows vs estimated rows if they are both available in the actual execution plan
        /// </summary>
        /// <returns>formatted string of actual rows vs estimated rows; or null if estimateRows or actualRows is null</returns>
        private string GetRowStatisticsDisplayString()
        {
            var actualRowsCounters = this[NodeBuilderConstants.ActualRows] as RunTimeCounters;
            ulong? actualRows = actualRowsCounters != null ? actualRowsCounters.TotalCounters : (ulong?)null;
            var estimateRows = this[NodeBuilderConstants.EstimateRows] as double?;
            var estimateExecutions = this[NodeBuilderConstants.EstimateExecutions] as double?;

            if (estimateRows != null)
            {
                if (estimateExecutions != null)
                {
                    estimateRows = estimateRows * estimateExecutions;
                }
                // we display estimate rows as integer so need round function
                estimateRows = Math.Round(estimateRows.Value);
            }

            return GetRowStatisticsDisplayString(actualRows, estimateRows);
        }

        /// <summary>
        /// Inner function to provide a string for the actual rows vs estimated rows if they are both available in the actual execution plan
        /// </summary>
        /// <param name="actualRows">actual rows</param>
        /// <param name="estimateRows">estimated rows</param>
        /// <returns>formatted string of actual rows vs estimated rows; or null if any of the arguments is null</returns>
        private string GetRowStatisticsDisplayString(ulong? actualRows, double? estimateRows)
        {
            if (!actualRows.HasValue || !estimateRows.HasValue)
            {
                return null;
            }

            // estimateRows should always to be positive, I just change it to 1 just in case since we need to calculate the percentage
            estimateRows = estimateRows > 0 ? estimateRows : 1;

            // get the difference in percentage
            var actualString = actualRows.Value.ToString();
            var estimateString = estimateRows.Value.ToString();
            int percent = 100;
            if (estimateRows > 0)
            {
                percent = (int)(100 * ((double)actualRows / estimateRows));
            }

            actualString = actualString.PadLeft(estimateString.Length);
            estimateString = estimateString.PadLeft(actualString.Length);

            return SR.ActualOfEstimated(actualString, estimateString, percent);
        }

        public string GetRowCountDisplayString()
        {
            var actualRowsCounters = this[NodeBuilderConstants.ActualRows] as RunTimeCounters;
            ulong? actualRows = actualRowsCounters != null ? actualRowsCounters.TotalCounters : (ulong?)null;
            if (actualRows != null)
            {
                return actualRows.Value.ToString();
            }
            var estimateRows = this[NodeBuilderConstants.EstimateRows] as double?;
            var estimateExecutions = this[NodeBuilderConstants.EstimateExecutions] as double?;

            if (estimateRows != null)
            {
                if (estimateExecutions != null)
                {
                    estimateRows = estimateRows * estimateExecutions;
                }
                // we display estimate rows as integer so need round function
                estimateRows = Math.Round(estimateRows.Value);
            }
            return estimateRows == null ? "" : estimateRows.Value.ToString();
        }

        public string GetNodeCostDisplayString()
        {
            double cost = this.RelativeCost * 100;
            string costText = "";
            if (!this.HasPDWCost || cost > 0)
            {
                if (this.graph != null && this.graph.NodeStmtMap.Count < Node.LargePlanNodeCount)
                {
                    cost = Math.Round(cost);
                }
                costText = cost.ToString("0.##") + "%";
            }
            return costText;
        }

        #endregion

        #region Private variables

        private double cost;
        private bool costCalculated;
        private double subtreeCost;
        private Operation operation;
        private PropertyDescriptorCollection properties;
        private List<Node> children;
        private readonly string objectProperty = NodeBuilderConstants.Object;
        private readonly string predicateProperty = NodeBuilderConstants.LogicalOp;
        private Node parent;
        private ShowPlanGraph graph;
        private Edge parentEdge;
        private List<Edge> childrenEdges;
        private Node root;

        /// <summary>
        /// List of Seek or Scan type operators that can be considered match
        /// </summary>
        private List<string> SeekOrScanPhysicalOpList = new List<string> { "IndexSeek", "TableScan", "IndexScan", "ColumnstoreIndexScan" };

        #endregion

        #region Constants

        public static readonly int LargePlanNodeCount = 20;

        #endregion

        public void AddChild(Node child)
        {
            Edge edge = new Edge(this, child);
            this.childrenEdges.Add(edge);
            child.parentEdge = edge;
            this.children.Add(child);
            child.parent = this;
        }

    }
}
