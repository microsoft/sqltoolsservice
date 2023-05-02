//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts
{
    /// <summary>
    /// Information describing a Node in the Object Explorer tree. 
    /// Contains information required to display the Node to the user and
    /// to know whether actions such as expanding children is possible
    /// the node 
    /// </summary>
    public class NodeInfo
    {
        /// <summary>
        /// Path identifying this node: for example a table will be at ["server", "database", "tables", "tableName"].
        /// This enables rapid navigation of the tree without the need for a global registry of elements.
        /// The path functions as a unique ID and is used to disambiguate the node when sending requests for expansion.
        /// A common ID is needed since processes do not share address space and need a unique identifier
        /// </summary>
        public string NodePath { get; set; }

        /// <summary>
        /// The path of the parent node. This is going to be used by the client side to determine the parent node.
        /// We are not referencing the parent node directly because the information needs to be passed between processes.
        /// </summary>
        public string ParentNodePath { get; set; }

        /// <summary>
        /// The type of the node - for example Server, Database, Folder, Table
        /// </summary>
        public string NodeType { get; set; }

        /// <summary>
        /// Label to display to the user, describing this node.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Node Sub type - for example a key can have type as "Key" and sub type as "PrimaryKey"
        /// </summary>
        public string NodeSubType { get; set; }

        /// <summary>
        /// Node status - for example login can be disabled/enabled
        /// </summary>
        public string NodeStatus { get; set; }

        /// <summary>
        /// Is this a leaf node (in which case no children can be generated) or
        /// is it expandable?
        /// </summary>
        public bool IsLeaf { get; set; }

        /// <summary>
        /// Object Metadata for smo objects to be used for scripting
        /// </summary>
        public ObjectMetadata Metadata { get; set; }

        /// <summary>
        /// Error message returned from the engine for a object explorer node failure reason, if any.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The object type of the node. e.g. Database, Server, Tables...
        /// </summary>
        public string ObjectType { get; set; }

        public NodeFilterProperty[] FilterableProperties { get; set; }
    }

    public class NodeFilterProperty 
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public NodeFilterPropertyDataType Type { get; set; }
        public string[] Choices { get; set; }
    }

    public enum NodeFilterPropertyDataType
    {
        String = 0,
		Number = 1,
		Boolean = 2,
		Date = 3,
		Choice = 4
    }

    public enum NodeFilterOperator
    {
        Equals = 0,
		NotEquals = 1,
		LessThan = 2,
		LessThanOrEquals = 3,
		GreaterThan = 4,
		GreaterThanOrEquals = 5,
		Between = 6,
		NotBetween = 7,
		Contains = 8,
		NotContains = 9,
    }

    public class NodeFilter
    {		
        /**
		 * The name of the filter property
		 */
        public string Name { get; set; }
		/**
		 * The operator of the filter property
		 */
        public NodeFilterOperator Operator { get; set; }
		/**
		 * The applied values of the filter property
		 */
        public JToken Value { get; set; }
    }
}
