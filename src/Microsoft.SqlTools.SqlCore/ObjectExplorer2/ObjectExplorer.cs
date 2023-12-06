//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;


namespace Microsoft.SqlTools.SqlCore.ObjectExplorer2
{
    public class ObjectExplorer
    {
        public ObjectMetadata[] metadata { get; set; }

        public TreeNode root { get; set; }

        public ObjectExplorer()
        {
        }

        public TreeNode[] getNodeByPath(string path, SqlConnection connection, bool refresh = false)
        {
            if (refresh || metadata == null)
            {
                LoadMetaData(connection);
            }

            
            TreeNode currentNode = root;

            while(currentNode.Path != path)
            {
                if (currentNode.Children == null)
                {
                    currentNode.LoadChildren(metadata);
                }

                currentNode = currentNode.Children.FirstOrDefault(node => path.StartsWith(node.Path));
                if (currentNode == null)
                {
                    return null;
                }
            }

            if(currentNode.IsLeaf)
            {
                throw new Exception("leaf node cannot be expanded");
            }
            currentNode.LoadChildren(this.metadata);
            return currentNode.Children.ToArray();
        }

        public void LoadMetaData(SqlConnection connection)
        {
            string[] ObjectExplorerQueries = ObjectExplorerModelQueries.Queries.Values.ToArray();
            string combinedQuery = string.Join(Environment.NewLine + "UNION ALL" + Environment.NewLine, ObjectExplorerQueries);
            using (SqlCommand command = new SqlCommand(combinedQuery, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    List<ObjectMetadata> metadata = new List<ObjectMetadata>();
                    while (reader.Read())
                    {
                        ObjectMetadata objectMetadata = new ObjectMetadata()
                        {
                            Name = reader.IsDBNull(reader.GetOrdinal("objectName")) ? "" : reader.GetString(reader.GetOrdinal("objectName")),
                            Type = reader.IsDBNull(reader.GetOrdinal("Type")) ? "" : reader.GetString(reader.GetOrdinal("Type")),
                            DisplayName = reader.IsDBNull(reader.GetOrdinal("displayName")) ? "" : reader.GetString(reader.GetOrdinal("displayName")),
                            SchemaName = reader.IsDBNull(reader.GetOrdinal("schemaName")) ? "" : reader.GetString(reader.GetOrdinal("schemaName")),
                            parentName = reader.IsDBNull(reader.GetOrdinal("parentName")) ? "" : reader.GetString(reader.GetOrdinal("parentName"))
                        };
                        metadata.Add(objectMetadata);
                    }

                    this.metadata = metadata.ToArray();
                }
            }

            root = new DatabaseNode(null, new ObjectMetadata() { Name = connection.Database, Type = "Database", DisplayName = connection.Database });
        }
    }

    public class ObjectMetadata
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string SchemaName { get; set; }
        public string parentName { get; set; }
    }
}