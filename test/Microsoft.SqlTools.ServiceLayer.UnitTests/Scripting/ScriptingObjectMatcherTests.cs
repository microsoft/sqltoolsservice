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
    public class ScriptingMatcherTests
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
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject[0],
                excludeCriteria: new ScriptingObject[0],
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(6, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeNone()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject(),
                excludeCriteria: new ScriptingObject(),
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(0, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeName()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Name = "Table1" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(2, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeNameWildcard()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Name = "*" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(6, results.Count());
        }

        public void ScriptingMatchIncludeNameWildcardPostfix()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Name = "Tab*" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeSchema()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Schema = "S2" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(2, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeSchemaWildcard()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Schema = "*" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(6, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeSchemaWildcardPostfixCriteria()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Schema = "S*" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(6, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeSchemaWildcardPostfix()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Schema = "S*" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: "Table",
                excludeTypes: "View",
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeType()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Type = "Table" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeNameAndSchema()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Schema = "S1", Name = "Table1" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(1, results.Count());
        }

        [Fact]
        public void ScriptingMatchIncludeSchemaAndType()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: new ScriptingObject { Type = "View", Schema = "S1" },
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(2, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeName()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Name = "Table1" },
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeNameWildcard()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Name = "*" },
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(0, results.Count());
        }

        public void ScriptingMatchExcludeNameWildcardPostfix()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Name = "Tab*" },
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeSchema()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Schema = "S2" },
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeSchemaWildcard()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Schema = "*" },
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(0, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeSchemaWildcardPostfix()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Schema = "S*" },
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(0, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeType()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Type = "Table" },
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(2, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeNameAndSchemaCriteria()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Schema = "S1", Name = "Table1" },
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(5, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeNameAndSchema()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Name = "Table1" },
                includeSchemas: null,
                excludeSchemas: "S1",
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(1, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeSchemaAndTypeCriteria()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: new ScriptingObject { Type = "View", Schema = "S1" },
                includeSchemas: null,
                excludeSchemas: null,
                includeTypes: null,
                excludeTypes: null,
                candidates: TestData);

            Assert.Equal<int>(4, results.Count());
        }

        [Fact]
        public void ScriptingMatchExcludeSchemaAndType()
        {
            IEnumerable<ScriptingObject> results = ScriptingObjectMatcher.Match(
                includeCriteria: null,
                excludeCriteria: null,
                includeSchemas: null,
                excludeSchemas: "S1",
                includeTypes: null,
                excludeTypes: "View",
                candidates: TestData);

            Assert.Equal<int>(2, results.Count());
        }

        [Fact]
        public void ScriptingObjectEquality()
        {
            ScriptingObject scriptingObject1 = new ScriptingObject { Type = "Table", Schema = "test", Name = "test_table" };
            ScriptingObject scriptingObject2 = new ScriptingObject { Type = "Table", Schema = "test", Name = "test_table" };
            ScriptingObject scriptingObject3 = null;
            Assert.Equal(scriptingObject1, scriptingObject2);
            Assert.False(scriptingObject1.Equals(scriptingObject3));
        }
    }

    public class ScriptingUtilsTests
    {
        private static string[] TestObjects = new string[]
        {
            "Table",
            "Table]",
            "]Table",
            "Tab]le",
            "",
            "]",
            "view"
        };

        private static string[] ExpectedObjects = new string[]
        {
            "Table",
            "Table]]",
            "]]Table",
            "Tab]]le",
            "",
            "]]",
            "view"
        };
        

        [Fact]
        public void TestQuoteObjectName()
        {
            for (int i = 0; i < TestObjects.Length; i++)
            {
                Assert.Equal(Scripter.ScriptingUtils.QuoteObjectName(TestObjects[i]), ExpectedObjects[i]);
            }
        }
    }
}
