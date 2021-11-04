//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph;


namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ShowPlan
{
    public class ShowPlanXMLTests
    {
        [Test]
        public async Task ParseXMLFileReturnsValidShowPlanGraph()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(ShowPlanXMLTests));
            Stream scriptStream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".ShowPlan.TestExecutionPlan.xml");
            StreamReader reader = new StreamReader(scriptStream);
            string text = reader.ReadToEnd();
            var showPlanGraphs = ShowPlanGraph.ParseShowPlanXML(text, ShowPlanType.Actual);
            Assert.AreEqual(1, showPlanGraphs.Length, "Single show plan graph not generated from the test xml file");
            var testShowPlanGraph = showPlanGraphs[0];
            Assert.NotNull(testShowPlanGraph, "graph should not be null");
            Assert.NotNull(testShowPlanGraph.Root, "graph should have a root");
        }
    }
}