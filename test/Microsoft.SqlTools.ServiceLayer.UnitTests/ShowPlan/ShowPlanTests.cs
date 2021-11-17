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
        [Test]
        public void ParseXMLFileReturnsValidShowPlanGraph()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(ShowPlanXMLTests));
            Stream scriptStream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".ShowPlan.TestExecutionPlan.xml");
            StreamReader reader = new StreamReader(scriptStream);
            string text = reader.ReadToEnd();
            var showPlanGraphs = ShowPlanGraphUtils.CreateShowPlanGraph(text);
            Assert.AreEqual(1, showPlanGraphs.Count, "exactly one show plan graph should be returned");
            Assert.NotNull(showPlanGraphs[0], "graph should not be null");
            Assert.NotNull(showPlanGraphs[0].Root, "graph should have a root");
        }
    }
}