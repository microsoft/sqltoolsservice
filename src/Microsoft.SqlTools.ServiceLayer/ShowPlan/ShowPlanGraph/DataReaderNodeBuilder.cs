//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph
{
    /// <summary>
    /// Base class for building hierarchy of Graph objects from ShowPlan Record Set
    /// </summary>
	internal abstract class DataReaderNodeBuilder: INodeBuilder
    {
        #region Constructor

        /// <summary>
        /// Initializes base class members.
        /// </summary>
        /// <param name="showPlanType">Show Plan type.</param>
        public DataReaderNodeBuilder() 
        {}

        #endregion

        #region INodeBuilder

        /// <summary>
        /// Builds one or more Graphs that
        /// represnet data from the data source.
        /// </summary>
        /// <param name="dataSource">Data Source.</param>
        /// <returns>An array of AnalysisServices Graph objects.</returns>
        public ShowPlanGraph[] Execute(object dataSource)
		{
            IDataReader reader = dataSource as IDataReader;

            if (reader == null)
            {
                Debug.Assert(false, "Unexpected ShowPlan source = " + dataSource.GetType().ToString());
                throw new ArgumentException(SR.Keys.UnknownShowPlanSource);
            }

            List<ShowPlanGraph> graphs = new List<ShowPlanGraph>();
            Dictionary<int, Node> currentGraphNodes = null;
            NodeBuilderContext context = null;

            object[] values = null;
            string[] names = GetPropertyNames();

            while (reader.Read())
            {
                ReadValues(reader, ref values);

                int nodeID = (int)values[NodeIdIndex];
                int parentNodeID = (int)values[ParentIndex];

                Node parentNode = null;

                if (parentNodeID == 0)
                {
                    // Starting a new graph
                    // First add an old graph to the list
                    if (context != null)
                    {
                        graphs.Add(context.Graph);
                    }

                    // Create new Context and new Nodes hashtable
                    context = new NodeBuilderContext(new ShowPlanGraph(), ShowPlanType, this);
                    currentGraphNodes = new Dictionary<int, Node>();
                }
                else
                {
                    parentNode = currentGraphNodes[parentNodeID];
                }

                // Create new node.
                Debug.Assert(context != null);
                Node node = CreateNode(nodeID, context);

                ParseProperties(node, names, values);
                SetNodeSpecialProperties(node);

                if (parentNode != null)
                {
                    parentNode.Children.AddLast(node);
                }

                // Add node to the hashtable
                // In some rare cases the graph may already
                // contain the node with the same ID.
                // This happens, for example, in a case of 
                // Table Spool node. In this case it is safe
                // to not add the node to the currentGraphNodes collection
                // because it isn't going to have any children (guaranteed)
                if (!currentGraphNodes.ContainsKey(nodeID))
                {
                    currentGraphNodes.Add(nodeID, node);
                }
            }

            // Add the last parsed graph to the list of graphs.
            if (context != null)
            {
                graphs.Add(context.Graph);
            }

            return graphs.ToArray();
        }

        #endregion

        #region Implementation

        /// <summary>
        /// Gets property names that correspond to values returned
        /// in each ShowPlan row.
        /// </summary>
        /// <returns>Array of property names</returns>
        protected abstract string[] GetPropertyNames();

        /// <summary>
        /// Gets index of Node Id in the recordset
        /// </summary>
        protected abstract int NodeIdIndex { get; }

        /// <summary>
        /// Gets index of Parent Id in the recordset
        /// </summary>
        protected abstract int ParentIndex { get; }

        /// <summary>
        /// Gets the ShowPlanType of hte resordset
        /// </summary>
        protected abstract ShowPlanType ShowPlanType{ get; }

        /// <summary>
        /// Sequentially reads all columns from IDataReader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="values"></param>
        private void ReadValues(IDataReader reader, ref object[] values)
        {
            if (values == null || reader.FieldCount != values.Length)
            {
                values = new object[reader.FieldCount];
            }

            // We specifically need to read values sequentially
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = reader.GetValue(i);
            }
        }

        /// <summary>
        /// Reads properties from the data row.
        /// </summary>
        /// <param name="node">Node which is populated with properties</param>
        /// <param name="names">Property names.</param>
        /// <param name="values">Property values.</param>
        private void ParseProperties(Node node, string[] names, object[] values)
        {
            int count = Math.Min(names.Length, values.Length);

            for (int i = 0; i < count; i++)
            {
                if (names[i] != null && !((values[i] is DBNull) || values[i] == null))
                {
                    node[names[i]] = values[i];
                }
            }
        }

        /// <summary>
        /// Sets special properties on the node.
        /// </summary>
        /// <param name="node">Node.</param>
        private static void SetNodeSpecialProperties(Node node)
        {
            // SubtreeCost is a special property that should be set separately
            node.SubtreeCost = GetNodeSubtreeCost(node);

            Operation resultOp;

            string nodeType = (string)node["StatementType"];
            if (string.Compare(nodeType, "PLAN_ROW", StringComparison.OrdinalIgnoreCase) != 0)
            {
                // This is a statement
                resultOp = OperationTable.GetStatement(nodeType);

                node["LogicalOp"] = resultOp.DisplayName;
                node["PhysicalOp"] = resultOp.DisplayName;

                // For statements, Argument is the same as the Statement text (if any defined)
                node["Argument"] = node["StatementText"];
            }
            else
            {
                // This is an operation node
                
                // Remove StatementText property
                PropertyDescriptor statementTextProperty = node.Properties["StatementText"];
                if (statementTextProperty != null)
                {
                    node.Properties.Remove(statementTextProperty);
                }

                // Special consideration for Argument property:
                // Try to parse Object name from it
                string argument = node["Argument"] as string;
                if (argument != null)
                {
                    Match match = argumentObjectExpression.Match(argument);
                    if (match != Match.Empty)
                    {
                        node["Object"] = match.Groups["Object"].Value;
                    }
                }

                string physicalOpType = node["PhysicalOp"] as string;
                string logicalOpType = node["LogicalOp"] as string;

                if (physicalOpType == null || logicalOpType == null)
                {
                    throw new FormatException(SR.Keys.UnknownShowPlanSource);
                }

                // Remove spaces and other special characters from physical and logical names
                physicalOpType = operatorReplaceExpression.Replace(physicalOpType, "");
                logicalOpType = operatorReplaceExpression.Replace(logicalOpType, "");

                Operation physicalOp = OperationTable.GetPhysicalOperation(physicalOpType);
                Operation logicalOp = OperationTable.GetLogicalOperation(logicalOpType);

                resultOp = logicalOp != null && logicalOp.Image != null && logicalOp.Description != null
                    ? logicalOp : physicalOp;

                node["LogicalOp"] = logicalOp.DisplayName;
                node["PhysicalOp"] = physicalOp.DisplayName;

                // EstimateExecutions = EstimateRebinds +  EstimateRewinds + 1
                if (node["EstimateRebinds"] != null && node["EstimateRewinds"] != null)
                {
                    double estimateRebinds = (double) node["EstimateRebinds"];
                    double estimateRewinds = (double) node["EstimateRewinds"];
                    node["EstimateExecutions"] = estimateRebinds + estimateRewinds + 1;
                }

                // EstimateRowsAllExecs = EstimateRows * EstimateExecutions
                double estimateRows = node["EstimateRows"] == null ? 0.0 : Convert.ToDouble(node["EstimateRows"]);
                double estimateExecutions = node["EstimateExecutions"] == null ? 0.0 : Convert.ToDouble(node["EstimateExecutions"]);
                double actualExecutions = node["ActualExecutions"] == null ? 0.0 : Convert.ToDouble(node["ActualExecutions"]);
                node["EstimateRowsAllExecs"] = estimateRows * estimateExecutions;
            }

            Debug.Assert(resultOp.Image != null);
            node.Operation = resultOp;
        }

        /// <summary>
        /// 'Factory' method for creating Nodes, allows for subclasses to override
        /// </summary>
        protected virtual Node CreateNode(int nodeId, NodeBuilderContext context)
        {
            return new Node(nodeId, context);
        }

        /// <summary>
        /// Determines node subtree cost from existing node properties.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Node subtree cost.</returns>
        private static double GetNodeSubtreeCost(Node node)
        {
            object value = node["TotalSubtreeCost"];
            return value != null ? Convert.ToDouble(value, CultureInfo.CurrentCulture) : 0;
        }

        #endregion

        #region Private members

        private static Regex operatorReplaceExpression = new Regex(@"[ \-]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex argumentObjectExpression = new Regex(@"OBJECT:\((?<Object>[^\)]*)\)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        #endregion
    }
}
