//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.SqlCore.SimpleObjectExplorer
{
    public static class ObjectExplorer
    {
        public static async Task<TreeNode> GetObjectExplorerModel(SqlConnection connection)
        {
            ObjectMetadata[] metdata = await FetchObjectExplorerMetadataTable(connection);
            TreeNode root = new DatabaseNode(null, new ObjectMetadata() { Name = connection.Database, Type = "Database", DisplayName = connection.Database });
            // Load all the children
            Stack<TreeNode> stack = new Stack<TreeNode>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                TreeNode currentNode = stack.Pop();
                if (currentNode.IsLeaf)
                {
                    continue;
                }
                currentNode.LoadChildren(metdata);
                if (currentNode.Children != null)
                {
                    foreach (TreeNode child in currentNode.Children)
                    {
                        stack.Push(child);
                    }
                }
            }
            return root;
        }

        private static async Task<ObjectMetadata[]> FetchObjectExplorerMetadataTable(SqlConnection connection)
        {
            string[] metadataQueries = ObjectExplorerModelQueries.Queries.Values.ToArray();
            string combinedQuery = string.Join(Environment.NewLine + "UNION ALL" + Environment.NewLine, metadataQueries);
            using (SqlCommand command = new SqlCommand(combinedQuery, connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    List<ObjectMetadata> metadata = new List<ObjectMetadata>();
                    while (reader.Read())
                    {
                        ObjectMetadata objectMetadata = new ObjectMetadata()
                        {
                            Name = reader.IsDBNull(reader.GetOrdinal("object_name")) ? "" : reader.GetString(reader.GetOrdinal("object_name")),
                            Type = reader.IsDBNull(reader.GetOrdinal("object_type")) ? "" : reader.GetString(reader.GetOrdinal("object_type")),
                            DisplayName = reader.IsDBNull(reader.GetOrdinal("display_name")) ? "" : reader.GetString(reader.GetOrdinal("display_name")),
                            Schema = reader.IsDBNull(reader.GetOrdinal("schema_name")) ? "" : reader.GetString(reader.GetOrdinal("schema_name")),
                            Parent = reader.IsDBNull(reader.GetOrdinal("parent_name")) ? "" : reader.GetString(reader.GetOrdinal("parent_name")),
                            SubType = reader.IsDBNull(reader.GetOrdinal("object_sub_type")) ? "" : reader.GetString(reader.GetOrdinal("object_sub_type"))
                        };
                        metadata.Add(objectMetadata);
                    }
                    return metadata.ToArray();
                }
            }
        }

        public static TreeNode[] GetNodeChildrenFromPath(TreeNode root, string path)
        {
            if (root == null)
            {
                return null;
            }
            if (path == "/")
            {
                return root.Children.ToArray();
            }
            TreeNode currentNode = root;
            while (currentNode.Path != path)
            {
                if (currentNode.Children == null)
                {
                    throw new Exception("Given path does not exist");
                }
                currentNode = currentNode.Children.FirstOrDefault(node => path.StartsWith(node.Path));
                if (currentNode == null)
                {
                    throw new Exception("Given path does not exist");
                }
            }
            if (currentNode.IsLeaf)
            {
                throw new Exception("leaf node cannot be expanded");
            }
            return currentNode.Children.ToArray();
        }
    }

    public class ObjectMetadata
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Schema { get; set; }
        public string Parent { get; set; }
        public string SubType { get; set; }
    }
}