//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    internal sealed class GroupBySchemaTests
    {
        Mock<DatabaseChildFactory> factory;
        Mock<TreeNode> node;

        [SetUp]
        public void init()
        {
            factory = new Mock<DatabaseChildFactory>();
            factory.SetupGet(c => c.ChildQuerierTypes).Returns(null as Type[]);
            factory.Setup(c => c.CreateChild(It.IsAny<TreeNode>(), It.IsAny<SqlSmoObject>())).Returns((TreeNode node, Schema obj) => {
                return new TreeNode(){
                    Label = obj.Name,
                    NodeType = nameof(NodeTypes.Schemas)
                };
            });
            factory.CallBase = true;
            Mock<SmoQueryContext> context = new Mock<SmoQueryContext>(new Server(), null);
            context.CallBase = true;
            context.Object.ValidFor = ValidForFlag.None;
            
            node = new Mock<TreeNode>();
            node.Setup(n => n.GetContext()).Returns(context.Object);
        }

        [Test]
        public void SchemaBasedFoldersExcludedWhenGroupBySchemaIsEnabled()
        {
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.SqlTools.ObjectExplorer = new ObjectExplorerSettings() { GroupBySchema = true };
            var children = factory.Object.Expand(node.Object, true, "TestDB", true, new System.Threading.CancellationToken());
            Assert.False(children.Any(c => c.Label == "Tables"), "Tables subfolder in database should be excluded when group by schema is enabled");
            Assert.False(children.Any(c => c.Label == "Views"), "Views subfolder in database should be excluded when group by schema is enabled");
            Assert.False(children.Any(c => c.Label == "Synonyms"), "Synonyms subfolder in database should be excluded when group by schema is enabled");
        }

        [Test]
        public void SchemaBasedFoldersIncludedWhenGroupBySchemaIsDisabled()
        {
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.SqlTools.ObjectExplorer = new ObjectExplorerSettings() { GroupBySchema = false };
            var children = factory.Object.Expand(node.Object, true, "TestDB", true, new System.Threading.CancellationToken());
            Assert.True(children.Any(c => c.Label == "Tables"), "Tables subfolder in database should be included when group by schema is disabled");
            Assert.True(children.Any(c => c.Label == "Views"), "Views subfolder in database should be included when group by schema is disabled");
            Assert.True(children.Any(c => c.Label == "Synonyms"), "Synonyms subfolder in database should be included when group by schema is disabled");
        }

        
    }

}