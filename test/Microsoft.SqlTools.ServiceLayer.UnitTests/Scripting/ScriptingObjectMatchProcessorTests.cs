//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Scripting
{
    public class ScriptingMatchProcessorTests
    {
        private static ScriptingObject Table_S1_Table1 = new ScriptingObject
        {
            Type = "Table",
            Schema = "S1",
            Name = "Table1",
        };

        private static ScriptingObject Table_S1_Table2 = new ScriptingObject
        {
            Type = "Table",
            Schema = "S1",
            Name = "Table2",
        };

        private static ScriptingObject Table_S2_Table1 = new ScriptingObject
        {
            Type = "Table",
            Schema = "S2",
            Name = "Table1",
        };

        private static ScriptingObject Table_S2_Table2 = new ScriptingObject
        {
            Type = "Table",
            Schema = "S2",
            Name = "Table2",
        };

        private static ScriptingObject View_S1_View1 = new ScriptingObject
        {
            Type = "View",
            Schema = "S1",
            Name = "View1",
        };

        private static ScriptingObject View_S1_View2 = new ScriptingObject
        {
            Type = "View",
            Schema = "S1",
            Name = "View2",
        };

        private static List<ScriptingObject> TestData = new List<ScriptingObject>
        {
            Table_S1_Table1,
            Table_S1_Table2,
            Table_S2_Table1,
            Table_S2_Table2,
            View_S1_View1,
            View_S1_View2,
        };

        [Fact]
        public void ScriptingMatchIncludeAll()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject[0],
                excludeCriteria: new ScriptingObject[0],
                candidates: TestData);

            Assert.Equal<int>(6, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeNone()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject(),
                excludeCriteria: new ScriptingObject(),
                candidates: TestData);

            Assert.Equal<int>(0, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeName()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject { Name = "Table1"},
                excludeCriteria: null,
                candidates: TestData);

            Assert.Equal<int>(2, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeNameWildcard()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject { Name = "*" },
                excludeCriteria: null,
                candidates: TestData);

            Assert.Equal<int>(6, results.Count());
        }

        public void ScriptingMatchIncludeNameWildcardPostfix()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject { Name = "Tab*" },
                excludeCriteria: null,
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeSchema()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject { Schema = "S2" },
                excludeCriteria: null,
                candidates: TestData);

            Assert.Equal<int>(2, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeSchemaWildcard()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject { Schema = "*" },
                excludeCriteria: null,
                candidates: TestData);

            Assert.Equal<int>(6, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeSchemaWildcardPostfix()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject { Schema = "S*" },
                excludeCriteria: null,
                candidates: TestData);

            Assert.Equal<int>(6, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeType()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject { Type="Table" },
                excludeCriteria: null,
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeNameAndSchema()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject { Schema = "S1", Name = "Table1" },
                excludeCriteria: null,
                candidates: TestData);

            Assert.Equal<int>(1, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeSchemaAndType()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: new ScriptingObject { Type="View", Schema = "S1" },
                excludeCriteria: null,
                candidates: TestData);

            Assert.Equal<int>(2, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeName()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Name = "Table1" },
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeNameWildcard()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Name = "*" },
                candidates: TestData);

            Assert.Equal<int>(0, results.Count());
        }

        public void ScriptingMatchExcludeNameWildcardPostfix()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Name = "Tab*" },
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeSchema()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Schema = "S2" },
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeSchemaWildcard()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Schema = "*" },
                candidates: TestData);

            Assert.Equal<int>(0, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeSchemaWildcardPostfix()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Schema = "S*" },
                candidates: TestData);

            Assert.Equal<int>(0, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeType()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Type = "Table" },
                candidates: TestData);

            Assert.Equal<int>(2, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeNameAndSchema()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Schema = "S1", Name = "Table1" },
                candidates: TestData);

            Assert.Equal<int>(5, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeSchemaAndType()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatchProcessor.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Type = "View", Schema = "S1" },
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }
    }
}
