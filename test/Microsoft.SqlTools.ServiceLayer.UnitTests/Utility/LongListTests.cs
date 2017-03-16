//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    /// <summary>
    /// Tests for the LongList class
    /// </summary>
    public class LongListTests
    {
        [Fact]
        public void LongListConstruction()
        {
            // If: I construct a new long list
            LongList<char> ll = new LongList<char>();

            // Then:
            // ... There should be no values in the list
            Assert.Equal(0, ll.Count);
        }

        #region GetItem / Add Tests

        [Theory]
        [InlineData(-1L)]   // Negative index
        [InlineData(0L)]    // Index equal to count of elements
        [InlineData(100L)]  // Index larger than elements
        public void GetItemOutOfRange(long index)
        {
            // If: I construct a new long list
            LongList<char> ll = new LongList<char>();

            // Then:
            // ... There should be no values in the list
            Assert.Throws<ArgumentOutOfRangeException>(() => ll[index]);
            Assert.Throws<ArgumentOutOfRangeException>(() => ll.GetItem(index));
        }

        [Theory]
        [InlineData(0)]    // Element at beginning
        [InlineData(1)]    // Element in middle
        [InlineData(2)]    // Element at end
        public void GetItemNotExpanded(long index)
        {
            // If: I construct a new long list with a couple items in it
            LongList<int> ll = new LongList<int> {0, 1, 2};

            // Then: I can read back the value from the list
            Assert.Equal(3, ll.Count);
            Assert.Equal(index, ll[index]);
            Assert.Equal(index, ll.GetItem(index));
        }

        [Fact]
        public void GetItemExanded()
        {
            // If: I construct a new long list that is guaranteed to have been expanded
            LongList<int> ll = new LongList<int> {ExpandListSize = 2};
            for (int i = 0; i < 10; i++)
            {
                ll.Add(i);
            }

            // Then:
            // ... All the added values should be accessible
            Assert.Equal(10, ll.Count);
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, ll[i]);
                Assert.Equal(i, ll.GetItem(i));
            }
        }

        #endregion

        #region SetItem Tests

        [Theory]
        [InlineData(-1L)]   // Negative index
        [InlineData(0L)]    // Index equal to count of elements
        [InlineData(100L)]  // Index larger than elements
        public void SetItemOutOfRange(long index)
        {
            // If: I construct a new long list
            LongList<int> ll = new LongList<int>();

            // Then:
            // ... There should be no values in the list
            Assert.Throws<ArgumentOutOfRangeException>(() => ll[index] = 8);
            Assert.Throws<ArgumentOutOfRangeException>(() => ll.SetItem(index, 8));
        }

        [Fact]
        public void SetItemNotExpanded()
        {
            // If:
            // ... I construct a new long list with a few items in it
            // ... And I set all values to new values
            LongList<int> ll = new LongList<int> {0, 1, 2};
            for (int i = 0; i < ll.Count; i++)
            {
                ll.SetItem(i, 8);
            }

            // Then: All values in the list should be 8
            Assert.All(ll, i => Assert.Equal(8, i));
        }

        [Fact]
        public void SetItemIndexerNotExpanded()
        {
            // If:
            // ... I construct a new long list with a few items in it
            // ... And I set all values to new values
            LongList<int> ll = new LongList<int> {0, 1, 2};
            for (int i = 0; i < ll.Count; i++)
            {
                ll[i] = 8;
            }

            // Then: All values in the list should be 8
            Assert.All(ll, i => Assert.Equal(8, i));
        }

        [Fact]
        public void SetItemExpanded()
        {
            // If:
            // ... I construct a new long list that is guaranteed to have been expanded
            LongList<int> ll = new LongList<int> {ExpandListSize = 2};
            for (int i = 0; i < 10; i++)
            {
                ll.Add(i);
            }

            // ... And reset all the values to 8
            for (int i = 0; i < 10; i++)
            {
                ll.SetItem(i, 8);
            }

            // Then: All values in the list should be 8
            Assert.All(ll, i => Assert.Equal(8, i));
        }

        [Fact]
        public void SetItemIndexerExpanded()
        {
            // If:
            // ... I construct a new long list that is guaranteed to have been expanded
            LongList<int> ll = new LongList<int> {ExpandListSize = 2};
            for (int i = 0; i < 10; i++)
            {
                ll.Add(i);
            }

            // ... And reset all the values to 8
            for (int i = 0; i < 10; i++)
            {
                ll[i] = 8;
            }

            // Then: All values in the list should be 8
            Assert.All(ll, i => Assert.Equal(8, i));
        }

        #endregion

        #region RemoveAt Tests

        [Theory]
        [InlineData(-1L)]   // Negative index
        [InlineData(0L)]    // Index equal to count of elements
        [InlineData(100L)]  // Index larger than elements
        public void RemoveOutOfRange(long index)
        {
            // If: I construct a new long list
            LongList<char> ll = new LongList<char>();

            // Then:
            // ... There should be no values in the list
            Assert.Throws<ArgumentOutOfRangeException>(() => ll.RemoveAt(index));
        }

        [Theory]
        [InlineData(0)]    // Remove at beginning of list
        [InlineData(2)]    // Remove from middle of list
        [InlineData(4)]    // Remove at end of list
        public void RemoveAtNotExpanded(long index)
        {
            // If:
            // ... I create a long list with a few elements in it (and one element that will be removed)
            LongList<int> ll = new LongList<int>();
            for (int i = 0; i < 5; i++)
            {
                ll.Add(i == index ? 1 : 8);
            }

            // ... And I delete an element
            ll.RemoveAt(index);

            // Then:
            // ... The count should have subtracted
            Assert.Equal(4, ll.Count);

            // ... All values should be 8 since we removed the 1
            Assert.All(ll, i => Assert.Equal(8, i));
        }

        [Fact]
        public void RemoveAtExpanded()
        {
            // If:
            // ... I create a long list that is guaranteed to be expanded
            //     (Created with 2x the values, evaluate the )
            LongList<int> ll = new LongList<int> {ExpandListSize = 2};
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    ll.Add(i);
                }
            }

            // ... And I delete all of the first half of values
            //     (we're doing this backwards to make sure remove works at different points in the list)
            for (int i = 9; i >= 0; i--)
            {
                ll.RemoveAt(i);
            }

            // Then:
            // ... The second half of the values should still remain
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, ll[i]);
            }

            // If:
            // ... I then proceed to add elements onto the end again
            for (int i = 0; i < 10; i++)
            {
                ll.Add(i);
            }

            // Then: All the elements should be there, in order
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    int index = j * 10 + i;
                    Assert.Equal(i, ll[index]);
                }
            }
        }

        #endregion

        #region IEnumerable Tests

        [Fact]
        public void GetEnumerator()
        {
            // Setup: Create a long list with a handful of elements
            LongList<int> ll = new LongList<int>();
            for (int i = 0; i < 5; i++)
            {
                ll.Add(i);
            }

            // If: I get iterate over the list via GetEnumerator
            // Then: All the elements should be returned, in order
            int val = 0;
            foreach (int element in ll)
            {
                Assert.Equal(val++, element);
            }
        }

        [Fact]
        public void GetEnumeratorExpanded()
        {
            // Setup: Create a long list with a handful of elements
            LongList<int> ll = new LongList<int> {ExpandListSize = 2};
            for (int i = 0; i < 5; i++)
            {
                ll.Add(i);
            }

            // If: I get iterate over the list via GetEnumerator
            // Then: All the elements should be returned, in order
            int val = 0;
            foreach (int element in ll)
            {
                Assert.Equal(val++, element);
            }
        }

        [Theory]
        [InlineData(-1)]    // Negative
        [InlineData(5)]     // Equal to count
        [InlineData(100)]   // Far too large
        public void LongSkipOutOfRange(long index)
        {
            // Setup: Create a long list with a handful of elements
            LongList<int> ll = new LongList<int> {ExpandListSize = 2};
            for (int i = 0; i < 5; i++)
            {
                ll.Add(i);
            }

            // If: I attempt to skip ahead by a value that is out of range
            // Then: I should get an exception
            // NOTE: We must do the .ToList in order to evaluate the LongSkip since it is implemented
            //       with a yield return
            Assert.Throws<ArgumentOutOfRangeException>(() => ll.LongSkip(index).ToArray());
        }

        [Theory]
        [InlineData(0)]    // Don't actually skip anything
        [InlineData(2)]    // Skip within the short list
        public void LongSkip(long index)
        {
            // Setup: Create a long list with a handful of elements
            LongList<int> ll = new LongList<int>();
            for (int i = 0; i < 5; i++)
            {
                ll.Add(i);
            }

            // If: I skip ahead by a few elements and get all elements in an array
            int[] values = ll.LongSkip(index).ToArray();

            // Then: The elements including the skip start index should be in the output
            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(ll[i+index], values[i]);
            }
        }

        [Theory]
        [InlineData(0)]    // Don't actually skip anything
        [InlineData(1)]    // Skip within the short list
        [InlineData(3)]    // Skip across expanded lists
        public void LongSkipExpanded(long index)
        {
            // Setup: Create a long list with a handful of elements
            LongList<int> ll = new LongList<int> {ExpandListSize = 2};
            for (int i = 0; i < 5; i++)
            {
                ll.Add(i);
            }

            // If: I skip ahead by a few elements and get all elements in an array
            int[] values = ll.LongSkip(index).ToArray();

            // Then: The elements including the skip start index should be in the output
            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(ll[i+index], values[i]);
            }
        }

        #endregion

        /// <summary>
        /// Add and remove and item in a LongList causing an expansion
        /// </summary>
        [Fact]
        public void LongListExpandTest()
        {
            var longList = new LongList<int> {ExpandListSize = 3};
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

