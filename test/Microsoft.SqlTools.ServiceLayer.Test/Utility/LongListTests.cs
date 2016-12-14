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

        /// <summary>
        /// Add and remove and item in a LongList causing an expansion
        /// </summary>
        [Fact]
        public void LongListExpandTest()
        {
            var longList = new LongList<int>();
            longList.ExpandListSize = 3;
            for (int i = 0; i < 6; ++i)
            {
                longList.Add(i);
            }
            Assert.Equal(longList.Count, 6);
            Assert.NotNull(longList.GetItem(4));
            
            bool didEnum = false;
            foreach (var j in longList)
            {            
                didEnum = true;
                break;
            }

            Assert.True(didEnum);

            longList.RemoveAt(4);
            Assert.Equal(longList.Count, 5);
        }
    }
}

