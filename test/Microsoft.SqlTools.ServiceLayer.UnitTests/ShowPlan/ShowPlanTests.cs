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


namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ShowPlan
{
    public class ShowPlanXMLTests
    {
        private string queryPlanFileText;

        [Test]
        public void ParseXMLFileReturnsValidShowPlanGraph()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
            var showPlanGraphs = ShowPlanGraphUtils.CreateShowPlanGraph(queryPlanFileText, "testFile.sql");
            Assert.AreEqual(1, showPlanGraphs.Count, "exactly one show plan graph should be returned");
            Assert.NotNull(showPlanGraphs[0], "graph should not be null");
            Assert.NotNull(showPlanGraphs[0].Root, "graph should have a root");
        }

        [Test]
        public void ParsingNestedProperties()
        {
            ReadFile(".ShowPlan.TestShowPlan.xml");
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
            ReadFile(".ShowPlan.TestShowPlanRecommendations.xml");
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