using System;
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Reflection;
using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.ShowPlan;


namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ShowPlan
{
    public class ShowPlanXMLTests
    {
        private string queryPlanFileText;

        [SetUp]
        public void LoadQueryPlanBeforeEachTest()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(ShowPlanXMLTests));
            Stream scriptStream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".ShowPlan.TestExecutionPlan.xml");
            StreamReader reader = new StreamReader(scriptStream);
            queryPlanFileText = reader.ReadToEnd();
        }

        [Test]
        public void ParseXMLFileReturnsValidShowPlanGraph()
        {

            var showPlanGraphs = ShowPlanGraphUtils.CreateShowPlanGraph(queryPlanFileText);
            Assert.AreEqual(1, showPlanGraphs.Count, "exactly one show plan graph should be returned");
            Assert.NotNull(showPlanGraphs[0], "graph should not be null");
            Assert.NotNull(showPlanGraphs[0].Root, "graph should have a root");
        }

        [Test]
        public void ParsingNestedProperties()
        {
            string[] commonNestedPropertiesNames = { "MemoryGrantInfo", "OptimizerHardwareDependentProperties" };
            var showPlanGraphs = ShowPlanGraphUtils.CreateShowPlanGraph(queryPlanFileText);
            ExecutionPlanNode rootNode = showPlanGraphs[0].Root;
            rootNode.Properties.ForEach(p =>
            {
                if (Array.FindIndex(commonNestedPropertiesNames, i => i.Equals(p.Name)) != -1 && (NestedExecutionPlanGraphProperty)p != null)
                {
                    Assert.NotZero(((NestedExecutionPlanGraphProperty)p).Value.Count);
                }
            });
        }
    }
}