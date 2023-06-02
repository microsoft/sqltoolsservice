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
        /// <summary>
        /// Filterable properties that this node supports
        /// </summary>
        public NodeFilterProperty[] FilterableProperties { get; set; }
    }

    /// <summary>
    /// Creates a NodeInfo and configures it for error situations
    /// </summary>
    public static class ErrorNodeInfo
    {
        /// <summary>
        /// Helper function to create an error node.
        /// </summary>
        /// <param name="parentNodePath">The parent node the error node will appear under</param>
        /// <param name="errorMessage">The error message to display in the error node</param>
        /// <returns>NodeInfo instance with the specified parent path and error message</returns>
        public static NodeInfo CreateErrorNode(string parentNodePath, string errorMessage)
        {
            return new NodeInfo()
            {
                ParentNodePath = parentNodePath,
                ErrorMessage = errorMessage,
                Label = errorMessage,
                ObjectType = "error",
                NodeType = "error",
                IsLeaf = true
            };
        }
    }

    /// <summary>
    /// The filterable properties that a node supports
    /// </summary>
    public class NodeFilterProperty
    {
        /// <summary>
        /// The name of the filter property
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The name of the filter property displayed to the user
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// The description of the filter property
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The data type of the filter property
        /// </summary>
        public NodeFilterPropertyDataType Type { get; set; }
        /// <summary>
        /// The list of choices for the filter property if the type is choice
        /// </summary>
        public NodeFilterPropertyChoice[] Choices { get; set; }
    }

    /// <summary>
    /// The data type of the filter property. Matches NodeFilterPropertyDataType enum in ADS : https://github.com/microsoft/azuredatastudio/blob/main/src/sql/azdata.proposed.d.ts#L1847-L1853
    /// </summary>
    public enum NodeFilterPropertyDataType
    {
        String = 0,
        Number = 1,
        Boolean = 2,
        Date = 3,
        Choice = 4
    }

    /// <summary>
    /// The operator of the filter property. Matches NodeFilterOperator  enum in ADS: https://github.com/microsoft/azuredatastudio/blob/main/src/sql/azdata.proposed.d.ts#L1855-L1868
    /// </summary>
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

    /// <summary>
    /// The filters that can be used to filter the nodes in an expand request. 
    /// </summary>
    public class NodeFilter
    {
        /// <summary>
        /// The name of the filter property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The operator of the filter property
        /// </summary>
        public NodeFilterOperator Operator { get; set; }

        /// <summary>
        /// The applied values of the filter property
        /// </summary>
        public JToken Value { get; set; }
    }

    /// <summary>
    /// The choice for the filter property if the type is choice
    /// </summary>
    public class NodeFilterPropertyChoice
    {
        /// <summary>
        /// The dropdown display value for the choice
        /// </summary>
        /// <value></value>
        public string DisplayName { get; set; }

        /// <summary>
        /// The value of the choice
        /// </summary>
        public string Value { get; set; }
    }
}
