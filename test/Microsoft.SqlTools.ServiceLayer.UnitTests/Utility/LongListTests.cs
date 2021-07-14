//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    /// <summary>
    /// Tests for the LongList class
    /// </summary>
    public class LongListTests
    {
        [Test]
        public void LongListConstruction()
        {
            // If: I construct a new long list
            LongList<char> ll = new LongList<char>();

            // Then:
            // ... There should be no values in the list
            Assert.AreEqual(0, ll.Count);
        }

        #region GetItem / Add Tests

        [Test]
        public void GetItemOutOfRange([Values(-1L, 0L, 100L)] long index)
        {
            // If: I construct a new long list
            LongList<char> ll = new LongList<char>();

            // Then:
            // ... There should be no values in the list
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = ll[index]; }) ;
            Assert.Throws<ArgumentOutOfRangeException>(() => ll.GetItem(index));
        }

        [Test]
        public void GetItemNotExpanded([Values(0,1,2)] long index)
        {
            // If: I construct a new long list with a couple items in it
            LongList<int> ll = new LongList<int> {0, 1, 2};

            // Then: I can read back the value from the list
            Assert.AreEqual(3, ll.Count);
            Assert.AreEqual(index, ll[index]);
            Assert.AreEqual(index, ll.GetItem(index));
        }

        [Test]
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
            Assert.AreEqual(10, ll.Count);
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, ll[i]);
                Assert.AreEqual(i, ll.GetItem(i));
            }
        }

        #endregion

        #region SetItem Tests

        [Test]
        public void SetItemOutOfRange([Values(-1L, 0L, 100L)] long index)
        {
            // If: I construct a new long list
            LongList<int> ll = new LongList<int>();

            // Then:
            // ... There should be no values in the list
            Assert.Throws<ArgumentOutOfRangeException>(() => ll[index] = 8);
            Assert.Throws<ArgumentOutOfRangeException>(() => ll.SetItem(index, 8));
        }

        [Test]
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

            Assert.That(ll, Has.All.EqualTo(8), "All values in the list should be 8");
        }

        [Test]
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

            Assert.That(ll, Has.All.EqualTo(8), "All values in the list should be 8");
        }

        [Test]
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

            Assert.That(ll, Has.All.EqualTo(8), "All values in the list should be 8");
        }

        [Test]
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

            Assert.That(ll, Has.All.EqualTo(8), "All values in the list should be 8");
        }

        #endregion

        #region RemoveAt Tests

        [Test]
        public void RemoveOutOfRange([Values(-1L, 0L, 100L)] long index)
        {
            // If: I construct a new long list
            LongList<char> ll = new LongList<char>();

            // Then:
            // ... There should be no values in the list
            Assert.Throws<ArgumentOutOfRangeException>(() => ll.RemoveAt(index));
        }

        [Test]
        public void RemoveAtNotExpanded([Values(0,2,4)] long index)
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
            Assert.AreEqual(4, ll.Count);

            Assert.That(ll, Has.All.EqualTo(8), "All values in the list should be 8 since we removed the 1");
        }

        [Test]
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
                Assert.AreEqual(i, ll[i]);
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
                    Assert.AreEqual(i, ll[index]);
                }
            }
        }

        #endregion

        #region IEnumerable Tests

        [Test]
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
                Assert.AreEqual(val++, element);
            }
        }

        [Test]
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
                Assert.AreEqual(val++, element);
            }
        }

        [Test]
        public void LongSkipOutOfRange([Values(-1,5,100)] long index)
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

        [Test]
        public void LongSkip([Values(0,2)] long index)
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
                Assert.AreEqual(ll[i+index], values[i]);
            }
        }

        [Test]
        public void LongSkipExpanded([Values(0,1,3)] long index)
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
                Assert.AreEqual(ll[i+index], values[i]);
            }
        }

        #endregion

        /// <summary>
        /// Add and remove and item in a LongList causing an expansion
        /// </summary>
        [Test]
        public void LongListExpandTest()
        {
            var longList = new LongList<int> {ExpandListSize = 3};
            for (int i = 0; i < 6; ++i)
            {
                longList.Add(i);
            }
            Assert.AreEqual(6, longList.Count);
            Assert.NotNull(longList.GetItem(4));
            
            bool didEnum = false;
            foreach (var j in longList)
            {            
                didEnum = true;
                break;
            }

            Assert.True(didEnum);

            longList.RemoveAt(4);
            Assert.AreEqual(5, longList.Count);
        }
    }
}

