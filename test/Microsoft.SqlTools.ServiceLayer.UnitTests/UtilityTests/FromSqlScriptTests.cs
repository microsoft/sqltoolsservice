//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.UtilityTests
{
    public class FromSqlScriptTests
    {
        #region DecodeMultipartIdentifier Tests

        public static IEnumerable<object[]> DecodeMultipartIdentifierTestData
        {
            get
            {
                yield return new object[] { "identifier", new[] { "identifier" } };
                yield return new object[] { "simple.split", new[] { "simple", "split" } };
                yield return new object[] { "multi.simple.split", new[] { "multi", "simple", "split" } };
                yield return new object[] { "[escaped]", new[] { "escaped" } };
                yield return new object[] { "[escaped].[split]", new[] { "escaped", "split" } };
                yield return new object[] { "[multi].[escaped].[split]", new[] { "multi", "escaped", "split" } };
                yield return new object[] { "[escaped]]characters]", new[] { "escaped]characters" } };
                yield return new object[] { "[multi]]escaped]]chars]", new[] { "multi]escaped]chars" } };
                yield return new object[] { "[multi]]]]chars]", new[] { "multi]]chars" } };
                yield return new object[] { "unescaped]chars", new[] { "unescaped]chars" } };
                yield return new object[] { "multi]unescaped]chars", new[] { "multi]unescaped]chars" } };
                yield return new object[] { "multi]]chars", new[] { "multi]]chars" } };
                yield return new object[] { "[escaped.dot]", new[] { "escaped.dot" } };
                yield return new object[] { "mixed.[escaped]", new[] { "mixed", "escaped" } };
                yield return new object[] { "[escaped].mixed", new[] { "escaped", "mixed" } };
                yield return new object[] { "dbo.[[].weird", new[] { "dbo", "[", "weird" } };
            }
        }

        [Test]
        [TestCaseSource(nameof(DecodeMultipartIdentifierTestData))]
        public void DecodeMultipartIdentifierTest(string input, string[] output)
        {
            // If: I decode the input
            string[] decoded = FromSqlScript.DecodeMultipartIdentifier(input);

            // Then: The output should match what was expected
            Assert.AreEqual(output, decoded);
        }

        [Test]

        public void DecodeMultipartIdentifierFailTest([Values(
            "[bracket]closed",
            "[bracket][closed",
            ".stuff",
            "."
            )] string input)
        {
            // If: I decode an invalid input
            // Then: It should throw an exception
            Assert.Throws<FormatException>(() => FromSqlScript.DecodeMultipartIdentifier(input));
        }

        #endregion

        private static readonly object[] unescaped =
        {
            new object[] {"(0)", "0" },
            new object[] {"((0))", "0" },
            new object[] {"('')", "" },
            new object[] {"('stuff')", "stuff" },
            new object[] {"(N'')", "" },
            new object[] {"(N'stuff')", "stuff" },
            new object[] {"('''stuff')", "'stuff" },
            new object[] {"(N'stu''''ff')", "stu''ff" },
        };

        [Test, TestCaseSource(nameof(unescaped))]
        public void UnescapeTest(string input, string output)
        {
            Assert.AreEqual(output, FromSqlScript.UnwrapLiteral(input));
        }

        private static readonly object[] bracketed =
        {
            new object[] {"[name]", true },
            new object[] {"[   name   ]", true },
            new object[] {"[na[[]me]", true },
            new object[] {"[]", true },
            new object[] {"name", false },
            new object[] {"[name", false},
            new object[] {"name]", false },
            new object[] {"[]name", false},
            new object[] {"name[]", false},
            new object[] {"[na]me", false },
        };

        [Test, TestCaseSource(nameof(bracketed))]
        public void BracketedIdentifierTest(string input, bool output)
        {
            Assert.AreEqual(output, FromSqlScript.IsIdentifierBracketed(input));
        }
    }
}