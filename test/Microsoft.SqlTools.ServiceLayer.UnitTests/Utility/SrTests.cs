//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public class SrTests
    {
        /// <summary>
        /// Add and remove and item in a LongList
        /// </summary>
        [Fact]
        public void SrPropertiesTest()
        {
            Assert.NotNull(ServiceLayer.SR.QueryServiceSubsetBatchNotCompleted);
            Assert.NotNull(ServiceLayer.SR.QueryServiceFileWrapperWriteOnly);
            Assert.NotNull(ServiceLayer.SR.QueryServiceFileWrapperNotInitialized);
            Assert.NotNull(ServiceLayer.SR.QueryServiceColumnNull);
        }
    }
}
