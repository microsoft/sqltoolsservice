//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
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
            this.children = new LinkedList<Node>();
            this.childrenEdges = new LinkedList<Edge>();
            this.LogicalOpUnlocName = null;
            this.PhysicalOpUnlocName = null;
            this.root = context.Graph.Root;
            if(this.root == null)
            {
                this.root = this;
            }
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
        public LinkedList<Node> Children
        {
            get { return this.children; }
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

        public Graph Graph
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

        #endregion

        #region Private variables

        private double cost;
        private bool costCalculated;
        private double subtreeCost;
        private Operation operation;
        private PropertyDescriptorCollection properties;
        private LinkedList<Node> children;
        private readonly string objectProperty = NodeBuilderConstants.Object;
        private readonly string predicateProperty = NodeBuilderConstants.LogicalOp;
        private Node parent;
        private Graph graph;
        private Edge parentEdge;
        private LinkedList<Edge> childrenEdges;
        private string nodeType;

        private Node root;

        /// <summary>
        /// List of Seek or Scan type operators that can be considered match
        /// </summary>
        private List<string> SeekOrScanPhysicalOpList = new List<string> { "IndexSeek", "TableScan", "IndexScan", "ColumnstoreIndexScan" };

        #endregion

        public void AddChild(Node child)
        {
            Edge edge = new Edge(this, child);
            this.childrenEdges.AddLast(edge);
            child.parentEdge = edge;
            this.children.AddLast(child);
            child.parent = this;
        }

    }
}
