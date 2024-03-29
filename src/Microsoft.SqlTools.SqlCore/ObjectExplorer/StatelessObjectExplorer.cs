//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
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
        /// <param name="serverConnection"> Server connection to use for expanding the node. It will be used only if parent is null </param>
        /// <param name="accessToken"> Access token to connect to the server. To be used in case of AAD based connections </param>
        /// <param name="nodePath"> Path of the node to expand. Will be used only if parent is null </param>
        /// <param name="serverInfo"> Server information </param>
        /// <param name="options"> Object explorer expansion options </param>
        /// <param name="filters"> Filters to be applied on the leaf nodes </param>
        /// <returns></returns> 
        /// </summary>
        public static async Task<TreeNode[]> Expand(ServerConnection serverConnection, string? nodePath, ObjectExplorerServerInfo serverInfo, ObjectExplorerOptions options, INodeFilter[]? filters = null)
        {
            using (var taskCancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var task = Task.Run(() =>
                    {
                        TreeNode? node;
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
                        if (node != null)
                        {
                            return node.Refresh(taskCancellationTokenSource.Token, null, filters);
                        }
                        else
                        {
                            throw new InvalidArgumentException($"Parent node not found for path {nodePath}");
                        }
                    });
                    return await RunExpandTask(task, taskCancellationTokenSource, options.OperationTimeoutSeconds);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }
        }

        /// <summary>
        /// Expands the given node and returns the child nodes.
        /// </summary>
        /// <param name="node"> Node to expand </param>
        /// <param name="options"> Object explorer expansion options </param>
        /// <param name="filters"> Filters to be applied on the leaf nodes </param>
        /// <param name="securityToken"> Security token to connect to the server. To be used in case of AAD based connections </param>
        /// <returns></returns>
        public static async Task<TreeNode[]> ExpandTreeNode(TreeNode node, ObjectExplorerOptions options, INodeFilter[]? filters = null)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            using (var taskCancellationTokenSource = new CancellationTokenSource())
            {
                var expandTask = Task.Run(() =>
                            {
                                SmoQueryContext nodeContext = node.GetContextAs<SmoQueryContext>() ?? throw new ArgumentException("Node does not have a valid context");

                                if (options.GroupBySchemaFlagGetter != null)
                                {
                                    nodeContext.GroupBySchemaFlag = options.GroupBySchemaFlagGetter;
                                }
                                return node.Refresh(taskCancellationTokenSource.Token, null, filters);
                            });

                return await RunExpandTask(expandTask, taskCancellationTokenSource, options.OperationTimeoutSeconds);
            }
        }

        private static async Task<TreeNode[]> RunExpandTask(Task<IList<TreeNode>> expansionTask, CancellationTokenSource taskCancellationTokenSource, int operationTimeoutSeconds)
        {
            if (await Task.WhenAny(expansionTask, Task.Delay(TimeSpan.FromSeconds(operationTimeoutSeconds))) == expansionTask)
            {
                if (taskCancellationTokenSource.IsCancellationRequested)
                {
                    throw new TimeoutException("The operation has timed out.");
                }
                return expansionTask.Result.ToArray();
            }
            else
            {
                throw new TimeoutException("The operation has timed out.");
            }
        }
    }
}