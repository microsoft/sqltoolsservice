//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public class ValidateTests
    {
        [Test]
        public void IsWithinRangeTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Validate.IsWithinRange("parameterName", 1, 2, 3));
        }

        [Test]
        public void IsLessThanTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Validate.IsLessThan("parameterName", 2, 1));
        }

        [Test]
        public void IsNotEqualTest()
        {
            Assert.Throws<ArgumentException>(() => Validate.IsNotEqual<int>("parameterName", 1, 1));
        }

        [Test]
        public void IsNullOrWhiteSpaceTest()
        {
            Assert.Throws<ArgumentException>(() => Validate.IsNotNullOrWhitespaceString("parameterName", null));
        }
    }
}
