//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Represents a folder node in the tree
    /// </summary>
    public class FolderNode : SmoTreeNode
    {
        internal static string GetSchemaGroupedDatabaseFolderNodePathName(NodeTypes nodeTypeId)
        {
            return $"Folder:{nodeTypeId}";
        }

        internal static bool TryGetSchemaGroupedDatabaseFolderNodePathName(string nodeName, out string nodePathName)
        {
            switch (nodeName)
            {
                case nameof(NodeTypes.BuiltInSchemas):
                case nameof(NodeTypes.Programmability):
                case nameof(NodeTypes.ExternalResources):
                case nameof(NodeTypes.ServiceBroker):
                case nameof(NodeTypes.Storage):
                case nameof(NodeTypes.Security):
                    nodePathName = $"Folder:{nodeName}";
                    return true;
                default:
                    nodePathName = string.Empty;
                    return false;
            }
        }

        public FolderNode()
        {
            this.NodeType = nameof(NodeTypes.Folder);
        }
        /// <summary>
        /// For folders, this copies the context of its parent if available
        /// </summary>
        /// <returns></returns>
        public override object GetContext()
        {
            return Parent?.GetContext();
        }

        /// <summary>
        /// For folders, searches for its parent's SMO object rather than copying for itself
        /// </summary>
        /// <returns><see cref="NamedSmoObject"/> from this parent's parent, or null if not found</returns>
        public override NamedSmoObject GetParentSmoObject()
        {
            return ParentAs<SmoTreeNode>()?.GetParentSmoObject();
        }
    }
}
