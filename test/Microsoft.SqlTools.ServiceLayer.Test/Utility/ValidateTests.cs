//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.Test.Utility
{
    public class ValidateTests
    {
        [Fact]
        public void IsWithinRangeTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Validate.IsWithinRange("parameterName", 1, 2, 3));
        }

        [Fact]
        public void IsLessThanTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Validate.IsLessThan("parameterName", 2, 1));
        }

        [Fact]
        public void IsNotEqualTest()
        {
            Assert.Throws<ArgumentException>(() => Validate.IsNotEqual<int>("parameterName", 1, 1));
        }

        [Fact]
        public void IsNullOrWhiteSpaceTest()
        {
            Assert.Throws<ArgumentException>(() => Validate.IsNotNullOrWhitespaceString("parameterName", null));
        }
    }
}