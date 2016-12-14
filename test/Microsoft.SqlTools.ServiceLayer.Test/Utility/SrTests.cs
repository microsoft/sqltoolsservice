//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer;
using Xunit;

namespace Microsoft.SqlTools.Test.Utility
{
    public class SrTests
    {
        /// <summary>
        /// Add and remove and item in a LongList
        /// </summary>
        [Fact]
        public void SrPropertiesTest()
        {
            Assert.NotNull(new SR());
            Assert.NotNull(SR.QueryServiceSubsetBatchNotCompleted);
            Assert.NotNull(SR.QueryServiceFileWrapperWriteOnly);
            Assert.NotNull(SR.QueryServiceFileWrapperNotInitialized);
            Assert.NotNull(SR.QueryServiceColumnNull);
            Assert.NotNull(new SR.Keys());   
        }
    }
}
