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
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer
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
        public static async Task<TreeNode[]> Expand(string connectionString, SecurityToken? accessToken, string nodePath, ObjectExplorerServerInfo serverInfo, ObjectExplorerOptions options, INodeFilter[]? filters = null)
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

                try
                {
                    return await Expand(connection, accessToken, nodePath, serverInfo, options, filters);
                }
                finally
                {
                    if (connection.IsOpen)
                    {
                        connection.Disconnect();
                    }
                }
            }
        }

        /// <summary>
        /// Expands the node at the given path and returns the child nodes. If parent is not null, it will skip expanding from the top and use the connection from the parent node.
        /// </summary>
        /// <param name="serverConnection"> Server connection to use for expanding the node. It will be used only if parent is null </param>
        /// <param name="accessToken"> Access token to connect to the server. To be used in case of AAD based connections </param>
        /// <param name="nodePath"> Path of the node to expand. Will be used only if parent is null </param>
        /// <param name="serverInfo"> Server information </param>
        /// <param name="options"> Object explorer expansion options </param>
        /// <param name="filters"> Filters to be applied on the leaf nodes </param>
        /// <param name="parent"> Optional parent node. If provided, it will skip expanding from the top and and use the connection from the parent node </param>
        /// <returns></returns> 
        /// </summary>
        

        public static async Task<TreeNode[]> Expand(ServerConnection serverConnection, SecurityToken? accessToken, string? nodePath, ObjectExplorerServerInfo serverInfo, ObjectExplorerOptions options, INodeFilter[]? filters = null, TreeNode parent = null)
        {
            using (var taskCancellationTokenSource = new CancellationTokenSource())
            {

                try
                {
                    var token = accessToken == null ? null : accessToken.Token;

                    var task = Task.Run(() =>
                    {
                        TreeNode? node;
                        if (parent == null)
                        {
                            ServerNode serverNode = new ServerNode(serverInfo, serverConnection, null, options.GroupBySchemaFlagGetter);
                            TreeNode rootNode = new DatabaseTreeNode(serverNode, serverInfo.DatabaseName);

                            if (nodePath == null || nodePath == string.Empty)
                            {
                                nodePath = rootNode.GetNodePath();
                            }
                            node = rootNode;
                            if (node == null)
                            {
                                // Return empty array if node is not found
                                return new TreeNode[0];
                            }
                            node = rootNode.FindNodeByPath(nodePath, true, taskCancellationTokenSource.Token);
                        }
                        else
                        {
                            node = parent;
                        }

                        if (node != null)
                        {
                            return node.Expand(taskCancellationTokenSource.Token, token, filters);
                        }
                        else
                        {
                            throw new InvalidArgumentException($"Parent node not found for path {nodePath}");
                        }
                    });


                    if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(options.OperationTimeoutSeconds))) == task)
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
                catch (Exception ex)
                {
                    throw ex;
                }

            }
        }

    }
}