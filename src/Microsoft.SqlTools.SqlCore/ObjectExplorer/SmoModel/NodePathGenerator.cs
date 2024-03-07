//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Reflection;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel
{
    public class NodePathGenerator
    {
        private static ServerExplorerTree TreeRoot { get; set; }

        private static Dictionary<string, HashSet<Node>> NodeTypeDictionary { get; set; }

        public static void Initialize()
        {
            if (TreeRoot != null)
            {
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var resource = assembly.GetManifestResourceStream("Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel.SmoTreeNodesDefinition.xml");
            var serializer = new XmlSerializer(typeof(ServerExplorerTree));
            NodeTypeDictionary = new Dictionary<string, HashSet<Node>>();
            using (var reader = new StreamReader(resource))
            {
#pragma warning disable CA3075
                TreeRoot = (ServerExplorerTree)serializer.Deserialize(reader);
#pragma warning restore CA3075
            }

            foreach (var node in TreeRoot.Nodes)
            {
                var containedType = node.ContainedType();
                if (containedType != null && node.Label() != string.Empty)
                {
                    if (!NodeTypeDictionary.ContainsKey(containedType))
                    {
                        NodeTypeDictionary.Add(containedType, new HashSet<Node>());
                    }
                    HashSet<Node> nodeSet;
                    NodeTypeDictionary.TryGetValue(containedType, out nodeSet);
                    nodeSet.Add(node);
                }
            }
            var serverNode = TreeRoot.Nodes.FirstOrDefault(node => node.Name == "Server");
            var serverSet = new HashSet<Node>();
            serverSet.Add(serverNode);
            NodeTypeDictionary.Add("Server", serverSet);
        }

        public static HashSet<string> FindNodePaths(IObjectExplorerSession objectExplorerSession, string typeName, string schema, string name, string databaseName, List<string> parentNames = null)
        {
            if (TreeRoot == null)
            {
                Initialize();
            }

            var returnSet = new HashSet<string>();
            HashSet<Node> matchingNodes;
            NodeTypeDictionary.TryGetValue(typeName, out matchingNodes);
            if (matchingNodes == null)
            {
                return returnSet;
            }

            var path = name;
            if (schema != null)
            {
                path = schema + "." + path;
            }

            path ??= "";

            foreach (var matchingNode in matchingNodes)
            {
                var paths = GenerateNodePath(objectExplorerSession, matchingNode, databaseName, parentNames, path);
                foreach (var newPath in paths)
                {
                    returnSet.Add(newPath);
                }
            }
            return returnSet;
        }

        private static HashSet<string> GenerateNodePath(IObjectExplorerSession objectExplorerSession, Node currentNode, string databaseName, List<string> parentNames, string path)
        {
            if (parentNames != null)
            {
                parentNames = parentNames.ToList();
            }

            if (currentNode.Name == "Server" || (currentNode.Name == "Database" && objectExplorerSession.Root.NodeType == "Database"))
            {
                var serverRoot = objectExplorerSession.Root;
                if (objectExplorerSession.Root.NodeType == "Database")
                {
                    serverRoot = objectExplorerSession.Root.Parent;
                    path = objectExplorerSession.Root.NodeValue + (path.Length > 0 ? ("/" + path) : "");
                }

                path = serverRoot.NodeValue + (path.Length > 0 ? ("/" + path) : "");
                var returnSet = new HashSet<string>();
                returnSet.Add(path);
                return returnSet;
            }

            var currentLabel = currentNode.Label();
            if (currentLabel != string.Empty)
            {
                path = currentLabel + "/" + path;
                var returnSet = new HashSet<string>();
                foreach (var parent in currentNode.ParentNodes())
                {
                    var paths = GenerateNodePath(objectExplorerSession, parent, databaseName, parentNames, path);
                    foreach (var newPath in paths)
                    {
                        returnSet.Add(newPath);
                    }
                }
                return returnSet;
            }
            else
            {
                var returnSet = new HashSet<string>();
                if (currentNode.ContainedType() == "Database" || currentNode.ContainedType() == "ExpandableSchema")
                {
                    path = databaseName + "/" + path;
                }
                else if (parentNames != null && parentNames.Count > 0)
                {
                    var parentName = parentNames.Last();
                    parentNames.RemoveAt(parentNames.Count - 1);
                    path = parentName + "/" + path;
                }
                else
                {
                    return returnSet;
                }

                foreach (var parentNode in currentNode.ParentNodes())
                {
                    var newPaths = GenerateNodePath(objectExplorerSession, parentNode, databaseName, parentNames, path);
                    foreach (var newPath in newPaths)
                    {
                        returnSet.Add(newPath);
                    }
                }

                return returnSet;
            }
        }

        [XmlRoot("ServerExplorerTree")]
        public class ServerExplorerTree
        {
            [XmlElement("Node", typeof(Node))]
            public List<Node> Nodes { get; set; }

            public Node GetNode(string name)
            {
                foreach (var node in this.Nodes)
                {
                    if (node.Name == name)
                    {
                        return node;
                    }
                }

                return null;
            }
        }

        public class Node
        {
            [XmlAttribute]
            public string Name { get; set; }

            [XmlAttribute]
            public string LocLabel { get; set; }

            [XmlAttribute]
            public string TreeNode { get; set; }

            [XmlAttribute]
            public string NodeType { get; set; }

            [XmlElement("Child", typeof(Child))]
            public List<Child> Children { get; set; }

            public HashSet<Node> ChildFolders()
            {
                var childSet = new HashSet<Node>();
                foreach (var child in this.Children)
                {
                    var node = TreeRoot.GetNode(child.Name);
                    if (node != null)
                    {
                        childSet.Add(node);
                    }
                }
                return childSet;
            }

            public string ContainedType()
            {
                if (this.TreeNode != null)
                {
                    return this.TreeNode.Replace("TreeNode", "");
                }
                else if (this.NodeType != null)
                {
                    return this.NodeType;
                }
                return null;
            }

            public Node ContainedObject()
            {
                var containedType = this.ContainedType();
                if (containedType == null)
                {
                    return null;
                }

                var containedNode = TreeRoot.GetNode(containedType);
                if (containedNode == this)
                {
                    return null;
                }

                return containedNode;
            }

            public string Label()
            {
                if (this.LocLabel.StartsWith("SR."))
                {
                    return SR.Keys.GetString(this.LocLabel.Remove(0, 3));
                }

                return string.Empty;
            }

            public HashSet<Node> ParentNodes()
            {
                var parentNodes = new HashSet<Node>();
                foreach (var node in TreeRoot.Nodes)
                {
                    if (this != node && (node.ContainedType() == this.Name || node.Children.Any(child => child.Name == this.Name)))
                    {
                        parentNodes.Add(node);
                    }
                }
                return parentNodes;
            }
        }

        public class Child
        {
            [XmlAttribute]
            public string Name { get; set; }
        }
    }
}