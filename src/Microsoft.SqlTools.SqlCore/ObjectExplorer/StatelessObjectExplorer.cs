//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.SqlCore.Connection;
using Microsoft.SqlTools.SqlCore.ObjectExplorer;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel;

namespace Microsoft.SqlTools.CoreSql.ObjectExplorer
{
    /// <summary>
    /// Stateless object explorer class can be used to handle object explorer requests without creating a session. It requires a connection string and a node path to query objects from the server.
    /// </summary>
    public class StatelessObjectExplorer
    {
        /// <summary>
        /// Expands the node at the given path and returns the child nodes.
        /// </summary>
        /// <param name="connectionString"> Connection string to connect to the server </param>
        /// <param name="accessToken"> Access token to connect to the server. To be used in case of AAD based connections </param>
        /// <param name="nodePath"> Path of the node to expand </param>
        /// <param name="serverInfo"> Server information </param>
        /// <param name="options"> Object explorer options </param>
        /// <param name="filters"> Filters to be applied on the leaf nodes </param>
        /// <returns> Array of child nodes </returns>
        /// <exception cref="ArgumentNullException"> Thrown when the parent node is not found </exception>
        /// <exception cref="TimeoutException"> Thrown when the operation times out.</exception> <summary>
        /// </summary>     
        public static TreeNode[] Expand(string connectionString, SecurityToken? accessToken, string nodePath, ObjectExplorerServerInfo serverInfo, ObjectExplorerOptions options, INodeFilter[]? filters = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                ServerConnection connection;
                if (accessToken != null)
                {
                    connection = new ServerConnection(conn, accessToken as IRenewableToken);
                }
                else
                {
                    connection = new ServerConnection(conn);
                }

                ServerNode serverNode = new ServerNode(serverInfo, connection, null, options.GroupBySchemaFlagGetter);

                TreeNode rootNode = new DatabaseTreeNode(serverNode, serverInfo.DatabaseName);

                if(nodePath == null || nodePath == string.Empty)
                {
                    nodePath = rootNode.GetNodePath();
                }

                using (var taskCancellationTokenSource = new CancellationTokenSource())
                {
                    TreeNode? node = rootNode;
                    if (node == null)
                    {
                        // Return empty array if node is not found
                        return new TreeNode[0];
                    }

                    if (Monitor.TryEnter(node.BuildingMetadataLock, options.OperationTimeout))
                    {
                        try
                        {
                            var token = accessToken == null ? null : accessToken.Token;

                            var task = Task.Run(() =>
                            {
                                var node = rootNode.FindNodeByPath(nodePath, true, taskCancellationTokenSource.Token);
                                if (node != null)
                                {
                                    return node.Expand(taskCancellationTokenSource.Token, token, filters);
                                } else 
                                {
                                    throw new InvalidArgumentException($"Parent node not found for path {nodePath}");
                                }
                            });

                            if (task.Wait(TimeSpan.FromSeconds(options.OperationTimeout)))
                            {
                                if (taskCancellationTokenSource.IsCancellationRequested)
                                {
                                    throw new TimeoutException("The operation has timed out.");
                                }
                                return task.Result.ToArray();
                            }
                            else
                            {
                                throw new TimeoutException("The operation has timed out.");
                            }
                        }
                        finally
                        {
                            if (connection.IsOpen)
                            {
                                connection.Disconnect();
                            }
                            Monitor.Exit(node.BuildingMetadataLock);
                        }
                    }
                    else
                    {
                        throw new TimeoutException("The operation has timed out. Could not acquire the lock to build metadata for the node.");
                    }
                }
            }
        }


    }
}