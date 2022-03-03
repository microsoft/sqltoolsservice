using System;
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.ShowPlan;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph.Comparison;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ShowPlan
{
    public class ShowPlanXMLTests
    {
        private string queryPlanFileText;

        [Test]
        public void ParseXMLFileReturnsValidShowPlanGraph()
        {
            ReadFile(".ShowPlan.TestExecutionPlan.xml");
            var showPlanGraphs = ShowPlanGraphUtils.CreateShowPlanGraph(queryPlanFileText, "testFile.sql");
            Assert.AreEqual(1, showPlanGraphs.Count, "exactly one show plan graph should be returned");
            Assert.NotNull(showPlanGraphs[0], "graph should not be null");
            Assert.NotNull(showPlanGraphs[0].Root, "graph should have a root");
        }

        [Test]
        public void CompareShowPlan_DuplicateSkeletons()
        {
            ReadFile(".ShowPlan.TestExecutionPlan.xml");
            ShowPlanGraph[] graphs = ShowPlanGraph.ParseShowPlanXML(queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);
            var skeletonNode2 = skeletonManager.CreateSkeleton(rootNode);

            var skeletonCompareResult = skeletonManager.AreSkeletonsEquivalent(skeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.AreEqual(true, skeletonCompareResult);
        }

        [Test]
        public void CompareShowPlan_TwoNullSkeletons()
        {
            Node firstRoot = null;
            Node secondRoot = null;

            var skeletonManager = new SkeletonManager();
            var firstSkeletonNode = skeletonManager.CreateSkeleton(firstRoot);
            var secondSkeletonNode = skeletonManager.CreateSkeleton(secondRoot);

            var skeletonCompareResult = skeletonManager.AreSkeletonsEquivalent(firstSkeletonNode, secondSkeletonNode, ignoreDatabaseName: true);

            Assert.AreEqual(true, skeletonCompareResult);
        }

        [Test]
        public void CompareShowPlan_ComparingSkeletonAgainstNull()
        {
            ReadFile(".ShowPlan.TestExecutionPlan.xml");
            ShowPlanGraph[] graphs = ShowPlanGraph.ParseShowPlanXML(queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);
            SkeletonNode skeletonNode2 = null;

            var skeletonCompareResult = skeletonManager.AreSkeletonsEquivalent(skeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.AreEqual(false, skeletonCompareResult);
        }

        [Test]
        public void CompareShowPlan_DifferentChildCount()
        {
            ReadFile(".ShowPlan.TestExecutionPlan.xml");
            ShowPlanGraph[] graphs = ShowPlanGraph.ParseShowPlanXML(queryPlanFileText, ShowPlanType.Unknown);
            var rootNode = graphs[0].Root;

            var skeletonManager = new SkeletonManager();
            var skeletonNode = skeletonManager.CreateSkeleton(rootNode);
            var skeletonNode2 = skeletonManager.CreateSkeleton(rootNode);
            skeletonNode2.Children.RemoveAt(skeletonNode2.Children.Count - 1);

            var skeletonCompareResult = skeletonManager.AreSkeletonsEquivalent(skeletonNode, skeletonNode2, ignoreDatabaseName: true);

            Assert.AreEqual(false, skeletonCompareResult);
        }

        [Test]
        public void ParsingNestedProperties()
        {
            ReadFile(".ShowPlan.TestExecutionPlan.xml");
            string[] commonNestedPropertiesNames = { "MemoryGrantInfo", "OptimizerHardwareDependentProperties" };
            var showPlanGraphs = ShowPlanGraphUtils.CreateShowPlanGraph(queryPlanFileText, "testFile.sql");
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
            ReadFile(".ShowPlan.TestExecutionPlanRecommendations.xml");
            string[] commonNestedPropertiesNames = { "MemoryGrantInfo", "OptimizerHardwareDependentProperties" };
            var showPlanGraphs = ShowPlanGraphUtils.CreateShowPlanGraph(queryPlanFileText, "testFile.sql");
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