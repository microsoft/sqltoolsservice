//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
        /// The type of the node - for example Server, Database, Folder, Table
        /// </summary>
        public string NodeType { get; set; }

        /// <summary>
        /// Label to display to the user, describing this node.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Is this a leaf node (in which case no children can be generated) or
        /// is it expandable?
        /// </summary>
        public bool IsLeaf { get; set; }
    }
}
