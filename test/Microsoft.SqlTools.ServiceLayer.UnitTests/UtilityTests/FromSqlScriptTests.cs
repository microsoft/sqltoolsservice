//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.UtilityTests
{
    public class FromSqlScriptTests
    {
        #region DecodeMultipartIdentifier Tests

        public static IEnumerable<object> DecodeMultipartIdentifierTestData
        {
            get
            {
                yield return new object[] {"identifier", new[] {"identifier"}};
                yield return new object[] {"simple.split", new[] {"simple", "split"}};
                yield return new object[] {"multi.simple.split", new[] {"multi", "simple", "split"}};
                yield return new object[] {"[escaped]", new[] {"escaped"}};
                yield return new object[] {"[escaped].[split]", new[] {"escaped", "split"}};
                yield return new object[] {"[multi].[escaped].[split]", new[] {"multi", "escaped", "split"}};
                yield return new object[] {"[escaped]]characters]", new[] {"escaped]characters"}};
                yield return new object[] {"[multi]]escaped]]chars]", new[] {"multi]escaped]chars"}};
                yield return new object[] {"[multi]]]]chars]", new[] {"multi]]chars"}};
                yield return new object[] {"unescaped]chars", new[] {"unescaped]chars"}};
                yield return new object[] {"multi]unescaped]chars", new[] {"multi]unescaped]chars"}};
                yield return new object[] {"multi]]chars", new[] {"multi]]chars"}};
                yield return new object[] {"[escaped.dot]", new[] {"escaped.dot"}};
                yield return new object[] {"mixed.[escaped]", new[] {"mixed", "escaped"}};
                yield return new object[] {"[escaped].mixed", new[] {"escaped", "mixed"}};
                yield return new object[] {"dbo.[[].weird", new[] {"dbo", "[", "weird"}};
            }
        }
        
        [Theory]
        [MemberData(nameof(DecodeMultipartIdentifierTestData))]
        public void DecodeMultipartIdentifierTest(string input, string[] output)
        {
            // If: I decode the input
            string[] decoded = FromSqlScript.DecodeMultipartIdentifier(input);

            // Then: The output should match what was expected
            Assert.Equal(output, decoded);
        }

        [Theory]
        [InlineData("[bracket]closed")]
        [InlineData("[bracket][closed")]
        [InlineData(".stuff")]
        [InlineData(".")]
        public void DecodeMultipartIdentifierFailTest(string input)
        {
            // If: I decode an invalid input
            // Then: It should throw an exception
            Assert.Throws<FormatException>(() => FromSqlScript.DecodeMultipartIdentifier(input));
        }

        #endregion
        
        [Theory]
        [InlineData("(0)", "0")]
        [InlineData("((0))", "0")]
        [InlineData("('')", "")]
        [InlineData("('stuff')", "stuff")]
        [InlineData("(N'')", "")]
        [InlineData("(N'stuff')", "stuff")]
        [InlineData("('''stuff')", "'stuff")]
        [InlineData("(N'stu''''ff')", "stu''ff")]
        public void UnescapeTest(string input, string output)
        {
            Assert.Equal(output, FromSqlScript.UnwrapLiteral(input));
        }

        [Theory]
        [InlineData("[name]", true)]
        [InlineData("[   name   ]", true)]
        [InlineData("[na[[]me]", true)]
        [InlineData("name", false)]
        [InlineData("[name", false)]
        [InlineData("name]", false)]
        [InlineData("[]name", false)]
        [InlineData("name[]", false)]
        [InlineData("[na]me", false)]
        public void BracketedIdentifierTest(string input, bool output)
        {
            Assert.Equal(output, FromSqlScript.IsIdentifierBracketed(input));
        }
    }
}