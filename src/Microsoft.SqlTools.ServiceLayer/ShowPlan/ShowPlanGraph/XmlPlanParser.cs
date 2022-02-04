//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph
{
    /// <summary>
    /// Class for enumerating FunctionType objects
    /// </summary>
    internal sealed class FunctionTypeItem
    {
        internal enum ItemType
        {
            Unknown,
            Udf,
            StoredProcedure
        };

        internal FunctionTypeItem(FunctionType function, ItemType type)
        {
            this.function = function;
            this.type = type;
        }

        internal FunctionType Function
        {
            get { return this.function; }
        }

        internal ItemType Type
        {
            get { return this.type; }
        }

        private FunctionType function;
        private ItemType type;
    }

    /// <summary>
    /// Base class for all Xml Execution plan node parsers.
    /// </summary>
    internal abstract class XmlPlanParser : ObjectParser
    {
        /// <summary>
        /// Parses a ShowPlan item and either creates a new Node or adds properties to
        /// the provided Node.
        /// </summary>
        /// <param name="item">Item being parsed.</param>
        /// <param name="parentNode">Existing node which is used as a property host or a parent for the new node.</param>
        /// <param name="context">Node builder context.</param>
        public static void Parse(object item, object parentItem, Node parentNode, NodeBuilderContext context)
        {
            XmlPlanParser parser = XmlPlanParserFactory.GetParser(item.GetType());

            if (parser != null)
            {
                Node node = null;

                if (parser.ShouldParseItem(item))
                {
                    node = parser.GetCurrentNode(item, parentItem, parentNode, context);
                    if (node != null)
                    {
                        // add node/statement mapping to the ShowPlanGraph
                        if (context != null && context.Graph != null && !context.Graph.NodeStmtMap.ContainsKey(node))
                        {
                            context.Graph.NodeStmtMap.Add(node, item);
                        }
                        parser.ParseProperties(item, node.Properties, context);
                    }
                    if(parentNode == null)
                    {
                        context.Graph.Root = node;
                    }
                }
                else
                {
                    node = parentNode;
                }

                foreach (object child in parser.GetChildren(item))
                {
                    XmlPlanParser.Parse(child, item, node, context);
                }

                if (node != parentNode)
                {
                    parser.SetNodeSpecialProperties(node);
                    if (parentNode != null)
                    {
                        parentNode.AddChild(node);
                    }
                }
            }
            else
            {
                Debug.Assert(false, "Unexpected run type = " + item.ToString());
                // Debug.LogExThrow(); {{removed from ssms}}
                throw new InvalidOperationException(SR.Keys.UnexpectedRunType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parentItem"></param>
        /// <param name="parentNode"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract Node GetCurrentNode(object item, object parentItem, Node parentNode, NodeBuilderContext context);
        
        /// <summary>
        /// Enumerates children items of the item being parsed.
        /// </summary>
        /// <param name="parsedItem">The item being parsed.</param>
        /// <returns>Enumeration.</returns>
        public virtual IEnumerable GetChildren(object parsedItem)
        {
            return EnumerateChildren(parsedItem);
        }

        /// <summary>
        /// Extracts FunctionType blocks.
        /// </summary>
        /// <param name="parsedItem">The item being parsed.</param>
        /// <returns>Enumeration.</returns>
        public virtual IEnumerable<FunctionTypeItem> ExtractFunctions(object parsedItem)
        {
            // By default - no functions
            yield break;
        }

        /// <summary>
        /// Determines whether this node should be parsed
        /// </summary>
        /// <param name="parsedItem">ShowPlan item</param>
        /// <returns></returns>
        protected virtual bool ShouldParseItem(object parsedItem)
        {
            // All items are parsed by default
            return true;
        }

        /// <summary>
        /// Updates node special properties such as Operator, Cost, SubtreeCost.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        protected virtual void SetNodeSpecialProperties(Node node)
        {
            if (node.Operation == null)
            {
                node.Operation = GetNodeOperation(node);
            }

            // Retrieve Subtree cost for this node
            node.SubtreeCost = GetNodeSubtreeCost(node);

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
            double actualExecutions = node["ActualExecutions"] == null ? 0.0 : ((RunTimeCounters)node["ActualExecutions"]).TotalCounters;

            //It's unlikely the total number of rows would exceed DBL_MAX = 1.8*(10^308), thus safe to not check overflow.
            node["EstimateRowsAllExecs"] = estimateRows * estimateExecutions;
        }

        /// <summary>
        /// Determines if the current property is used to reference a child item.
        /// Hierarchy properties are skipped when property wrappers are being created.
        /// </summary>
        /// <param name="property">Property subject to test.</param>
        /// <returns>True if the property is a hierarchy property;
        /// false if this is a regular property that should appear in the property grid.
        /// </returns>
        protected override bool ShouldSkipProperty(PropertyDescriptor property)
        {
            Type type = property.PropertyType;

            if (type.IsArray)
            {
                type = type.GetElementType();
            }

            return XmlPlanParserFactory.GetParser(type) != null;
        }

        /// <summary>
        /// Determines Operation that corresponds to the object being parsed.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Operation that corresponds to the node.</returns>
        protected virtual Operation GetNodeOperation(Node node)
        {
            // STrace.Assert(false, "GetNodeOperation should not be called on base class."); {{aasim useless edit}}
            // STrace.LogExThrow(); {{aasim useless edit}}
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Determines node subtree cost from existing node properties.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Node subtree cost.</returns>
        protected virtual double GetNodeSubtreeCost(Node node)
        {
            // STrace.Assert(false, "GetNodeSubtreeCost should not be called because it isn't defined for all node types"); {{aasim useless edit}}
            // STrace.LogExThrow(); {{aasim useless edit}}
            throw new InvalidOperationException();
        }

        /// <summary>
        /// This method gets children in a generic way.
        /// It should be avoided in the cases where performance matters.
        /// </summary>
        /// <param name="parsedItem">Item to enumerate children for</param>
        /// <returns>Enumeration of children</returns>
        public static IEnumerable EnumerateChildren(object parsedItem)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(parsedItem))
            {
                if (Type.GetTypeCode(property.PropertyType) == TypeCode.Object)
                {
                    object value = property.GetValue(parsedItem);
                    if (value == null)
                    {
                        continue;
                    }

                    if (value is IEnumerable)
                    {
                        foreach (object item in (IEnumerable)value)
                        {
                            if (XmlPlanParserFactory.GetParser(item.GetType()) != null)
                            {
                                yield return item;
                            }
                        }
                    }
                    else
                    {
                        if (XmlPlanParserFactory.GetParser(value.GetType()) != null)
                        {
                            yield return value;
                        }
                    }
                }
            }

            yield break;
        }

        /// <summary>
        /// Creates a new Node.
        /// </summary>
        /// <param name="context">NodeBuilderContext.</param>
        /// <returns>New node instance.</returns>
        public static Node NewNode(NodeBuilderContext context)
        {
            XmlPlanNodeBuilder nodeBuilder = context.Context as XmlPlanNodeBuilder;
            Debug.Assert(nodeBuilder != null);
            
            // We don't use "NodeId" property of the Node here because
            // not all nodes have Id and the same Id can repeat in different
            // statement branches
            return new Node(nodeBuilder.GetCurrentNodeId(), context);
        }
    }
}
