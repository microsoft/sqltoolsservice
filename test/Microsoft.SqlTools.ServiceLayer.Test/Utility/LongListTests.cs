//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.Test.Utility
{
    /// <summary>
    /// Tests for the LongList class
    /// </summary>
    public class LongListTests
    {
        /// <summary>
        /// Add and remove and item in a LongList
        /// </summary>
        [Fact]
        public void LongListTest()
        {
            var longList = new LongList<char>();
            longList.Add('.');
            Assert.True(longList.Count == 1);
            longList.RemoveAt(0);
            Assert.True(longList.Count == 0);
        }
    }
}
