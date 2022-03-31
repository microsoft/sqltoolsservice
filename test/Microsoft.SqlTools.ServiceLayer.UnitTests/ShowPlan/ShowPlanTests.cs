//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan.Comparison;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ShowPlan
{
    public class ShowPlanXMLTests
    {
        private string queryPlanFileText;

        [Test]
        public void ParseXMLFileReturnsValidShowPlanGraph()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var showPlanGraphs = ExecutionPlanGraphUtils.CreateShowPlanGraph(queryPlanFileText, "testFile.sql");
            Assert.AreEqual(1, showPlanGraphs.Count, "exactly one show plan graph should be returned");
            Assert.NotNull(showPlanGraphs[0], "graph should not be null");
            Assert.NotNull(showPlanGraphs[0].Root, "graph should have a root");
        }

        [Test]
        public void CompareShowPlan_CreateSkeleton()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            Assert.NotNull(rootNode, "graph should have a root");

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);

            Assert.NotNull(skeletonNode, "skeletonNode should not be null");
        }

        [Test]
        public void CompareShowPlan_CreateSkeletonUsingDefaultConfiguredNode()
        {
            var graph = new ShowPlanGraph()
            {
                Root = null,
                Description = new Description()
            };
            var context = new NodeBuilderContext(graph, ShowPlanType.Unknown, null);
            var node = new Node(default, context);

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(node);

            Assert.NotNull(skeletonNode, "skeletonNode should not be null");
        }

        [Test]
        public void CompareShowPlan_DuplicateSkeletons()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);
            var skeletonNode2 = skeletonManager.CreateSkeleton(rootNode);

            var skeletonCompareResult = skeletonManager.AreSkeletonsEquivalent(skeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.IsTrue(skeletonCompareResult);
        }

        [Test]
        public void CompareShowPlan_TwoDefaultConfiguredSkeletons()
        {
            var graph = new ShowPlanGraph()
            {
                Root = null,
                Description = new Description()
            };
            var context = new NodeBuilderContext(graph, ShowPlanType.Unknown, null);
            var firstRoot = new Node(default, context);
            var secondRoot = new Node(default, context);

            var skeletonManager = new SkeletonManager();
            var firstSkeletonNode = skeletonManager.CreateSkeleton(firstRoot);
            var secondSkeletonNode = skeletonManager.CreateSkeleton(secondRoot);

            var skeletonCompareResult = skeletonManager.AreSkeletonsEquivalent(firstSkeletonNode, secondSkeletonNode, ignoreDatabaseName: true);

            Assert.IsTrue(skeletonCompareResult, "The two compared skeleton nodes should be equivalent");
        }

        [Test]
        public void CompareShowPlan_ComparingSkeletonAgainstNull()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);
            SkeletonNode skeletonNode2 = null;

            var skeletonCompareResult = skeletonManager.AreSkeletonsEquivalent(skeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.IsFalse(skeletonCompareResult);
        }

        [Test]
        public void CompareShowPlan_DifferentSkeletonChildCount()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);
            var skeletonNode2 = skeletonManager.CreateSkeleton(rootNode);
            skeletonNode2.Children.RemoveAt(skeletonNode2.Children.Count - 1);

            var skeletonCompareResult = skeletonManager.AreSkeletonsEquivalent(skeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.IsFalse(skeletonCompareResult);
        }

        [Test]
        public void CompareShowPlan_ColorMatchingSectionsWithEquivalentSkeletons()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);
            var skeletonNode2 = skeletonManager.CreateSkeleton(rootNode);

            Assert.IsFalse(skeletonNode.HasMatch);
            Assert.IsFalse(skeletonNode2.HasMatch);

            Assert.Zero(skeletonNode.MatchingNodes.Count);
            Assert.Zero(skeletonNode2.MatchingNodes.Count);

            skeletonManager.ColorMatchingSections(skeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.IsTrue(skeletonNode.HasMatch);
            Assert.IsTrue(skeletonNode2.HasMatch);

            Assert.AreEqual(1, skeletonNode.MatchingNodes.Count);
            Assert.AreEqual(1, skeletonNode2.MatchingNodes.Count);
        }

        [Test]
        public void CompareShowPlan_ColorMatchingSectionsWithNullAndNonNullSkeleton()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            SkeletonNode nullSkeletonNode = null;
            var skeletonNode2 = skeletonManager.CreateSkeleton(rootNode);

            skeletonManager.ColorMatchingSections(nullSkeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.IsNull(nullSkeletonNode);
            Assert.IsFalse(skeletonNode2.HasMatch);
            Assert.Zero(skeletonNode2.MatchingNodes.Count);
        }

        [Test]
        public void CompareShowPlan_ColorMatchingSectionsWithTwoNullSKeletons()
        {
            var skeletonManager = new SkeletonManager();
            SkeletonNode skeletonNode = null;
            SkeletonNode skeletonNode2 = null;

            skeletonManager.ColorMatchingSections(skeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.IsNull(skeletonNode);
            Assert.IsNull(skeletonNode2);
        }

        [Test]
        public void CompareShowPlan_ColorMatchingSectionsWithNonNullAndNullSkeleton()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);
            SkeletonNode nullSkeletonNode = null;

            skeletonManager.ColorMatchingSections(skeletonNode, nullSkeletonNode, ignoreDatabaseName: true);

            Assert.IsFalse(skeletonNode.HasMatch);
            Assert.Zero(skeletonNode.MatchingNodes.Count);
            Assert.IsNull(nullSkeletonNode);
        }

        [Test]
        public void CompareShowPlan_ColorMatchingSectionsWithDifferentChildCount()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);
            var skeletonNode2 = skeletonManager.CreateSkeleton(rootNode);
            skeletonNode2.Children.RemoveAt(skeletonNode2.Children.Count - 1);

            Assert.IsFalse(skeletonNode.Children[0].HasMatch);
            Assert.IsFalse(skeletonNode2.Children[0].HasMatch);

            Assert.Zero(skeletonNode.Children[0].MatchingNodes.Count);
            Assert.Zero(skeletonNode2.Children[0].MatchingNodes.Count);

            skeletonManager.ColorMatchingSections(skeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.IsTrue(skeletonNode.Children[0].HasMatch);
            Assert.IsTrue(skeletonNode2.Children[0].HasMatch);

            Assert.AreEqual(1, skeletonNode.Children[0].MatchingNodes.Count);
            Assert.AreEqual(1, skeletonNode2.Children[0].MatchingNodes.Count);
        }

        [Test]
        public void CompareShowPlan_FindNextNonIgnoreNode()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var result = skeletonManager.FindNextNonIgnoreNode(rootNode);

            Assert.NotNull(result);
            Assert.AreEqual("InnerJoin", result.LogicalOpUnlocName);
        }

        [Test]
        public void CompareShowPlan_FindNextNonIgnoreNodeWithChildlessRoot()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var graphs = ShowPlanGraph.ParseShowPlanXML(this.queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            for (int childIndex = 0; childIndex < rootNode.Children.Count; ++childIndex)
            {
                rootNode.Children.RemoveAt(0);
            }

            var skeletonManager = new SkeletonManager();
            var result = skeletonManager.FindNextNonIgnoreNode(rootNode);

            Assert.IsNull(result);
        }

        [Test]
        public void CompareShowPlan_FindNextNonIgnoreNodeWithNullNode()
        {
            Node rootNode = null;

            var skeletonManager = new SkeletonManager();
            var result = skeletonManager.FindNextNonIgnoreNode(rootNode);

            Assert.IsNull(result);
        }

        [Test]
        public void ParsingNestedProperties()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            string[] commonNestedPropertiesNames = { "MemoryGrantInfo", "OptimizerHardwareDependentProperties" };
            var showPlanGraphs = ExecutionPlanGraphUtils.CreateShowPlanGraph(queryPlanFileText, "testFile.sql");
            ExecutionPlanNode rootNode = showPlanGraphs[0].Root;
            rootNode.Properties.ForEach(p =>
            {
                if (Array.FindIndex(commonNestedPropertiesNames, i => i.Equals(p.Name)) != -1 && (NestedExecutionPlanGraphProperty)p != null)
                {
                    Assert.NotZero(((NestedExecutionPlanGraphProperty)p).Value.Count);
                }
            });
        }

        [Test]
        public void ParseXMLFileWithRecommendations()
        {
            //The first graph in this execution plan has 3 recommendations
            ReadFile(".ShowPlan.TestShowPlanRecommendations.xml");
            string[] commonNestedPropertiesNames = { "MemoryGrantInfo", "OptimizerHardwareDependentProperties" };
            var showPlanGraphs = ExecutionPlanGraphUtils.CreateShowPlanGraph(queryPlanFileText, "testFile.sql");
            List<ExecutionPlanRecommendation> rootNode = showPlanGraphs[0].Recommendations;
            Assert.AreEqual(3, rootNode.Count, "3 recommendations should be returned by the showplan parser");
        }

        private void ReadFile(string fileName)
        {
            Assembly assembly = Assembly.GetAssembly(typeof(ShowPlanXMLTests));
            Stream scriptStream = assembly.GetManifestResourceStream(assembly.GetName().Name + fileName);
            StreamReader reader = new StreamReader(scriptStream);
            queryPlanFileText = reader.ReadToEnd();
        }
    }
}