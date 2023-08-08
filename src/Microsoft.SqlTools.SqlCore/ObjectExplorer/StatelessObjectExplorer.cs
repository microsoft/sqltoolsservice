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
    public class StatelessObjectExplorer
    {
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

                ServerNode serverNode = new ServerNode(serverInfo, connection);

                TreeNode rootNode = new DatabaseTreeNode(serverNode, serverInfo.DatabaseName);

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
                                    throw new ArgumentNullException("Parent node not found.");
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