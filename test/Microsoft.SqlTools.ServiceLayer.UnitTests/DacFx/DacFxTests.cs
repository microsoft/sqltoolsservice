//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DacFx
{
    [TestFixture]
    public class DacFxTests
    {
        [Test]
        public void ExtractParseVersionShouldThrowExceptionGivenInvalidVersion()
        {
            string invalidVersion = "invalidVerison";
            Assert.Throws<ArgumentException>(() => ExtractOperation.ParseVersion(invalidVersion));
        }
    }
}
