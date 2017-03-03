//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// Collection class that permits storage of over <c>int.MaxValue</c> items. This is performed
    /// by using a 2D list of lists. The internal lists are only initialized as necessary. This
    /// collection implements IEnumerable to make it easier to run LINQ queries against it.
    /// </summary>
    /// <remarks>
    /// This class is based on code from $\Data Tools\SSMS_Main\sql\ssms\core\DataStorage\ArrayList64.cs
    /// with additions to bring it up to .NET 4.5 standards
    /// </remarks>
    /// <typeparam name="T">Type of the values to store</typeparam>
    public class LongList<T> : IEnumerable<T>
    {        
        #region Member Variables
        
        private int expandListSize = int.MaxValue;
        private List<List<T>> expandedList;
        private readonly List<T> shortList;

        #endregion

        /// <summary>   
        /// Creates a new long list
        /// </summary>
        public LongList()
        {
            shortList = new List<T>();
            Count = 0;
        }

        #region Properties

        /// <summary>
        /// The total number of elements in the array
        /// </summary>
        public long Count { get; private set; }

        public T this[long index]
        {
            get 
            { 
                return GetItem(index); 
            }

            set
            {
                SetItem(index, value);
            }
        }

        public int ExpandListSize
        {
            get 
            { 
                return this.expandListSize; 
            }
            internal set 
            { 
                this.expandListSize = value; 
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the specified value to the end of the list
        /// </summary>
        /// <param name="val">Value to add to the list</param>
        /// <returns>Index of the item that was just added</returns>
        public long Add(T val)
        {
            if (Count <= this.ExpandListSize)
            {
                shortList.Add(val);
            }
            else // need to split values into several arrays 
            {
                if (expandedList == null)
                {
                    // very inefficient so delay as much as possible
                    // immediately add 0th array
                    expandedList = new List<List<T>> {shortList};
                }

                int arrayIndex = (int)(Count / this.ExpandListSize); // 0 based

                List<T> arr;
                if (expandedList.Count <= arrayIndex) // need to make a new array
                {
                    arr = new List<T>();
                    expandedList.Add(arr);
                }
                else // use existing array
                {
                    arr = expandedList[arrayIndex];
                }
                arr.Add(val);
            }
            return (++Count);
        }

        /// <summary>
        /// Returns the item at the specified index
        /// </summary>
        /// <param name="index">Index of the item to return</param>
        /// <returns>The item at the index specified</returns>
        public T GetItem(long index)
        {
            T val = default(T);

            if (Count <= this.ExpandListSize)
            {
                int i32Index = Convert.ToInt32(index);
                val = shortList[i32Index];
            }
            else
            {
                int iArray32Index = (int) (Count / this.ExpandListSize);
                if (expandedList.Count > iArray32Index)
                {
                    List<T> arr = expandedList[iArray32Index];

                    int i32Index = (int) (Count % this.ExpandListSize);
                    if (arr.Count > i32Index)
                    {
                        val = arr[i32Index];
                    }
                }
            }
            return val;
        }

        /// <summary>
        /// Sets the item at the specified index
        /// </summary>
        /// <param name="index">Index of the item to set</param>
        /// <param name="value">The item to store at the index specified</param>
        public void SetItem(long index, T value)
        {
            Validate.IsLessThan(nameof(index), index, Count);

            if (Count <= this.ExpandListSize)
            {
                int i32Index = Convert.ToInt32(index);
                shortList[i32Index] = value;
            }
            else
            {
                int iArray32Index = (int) (Count / this.ExpandListSize);
                List<T> arr = expandedList[iArray32Index];

                int i32Index = (int)(Count % this.ExpandListSize);
                arr[i32Index] = value;
            }
        }

        /// <summary>
        /// Removes an item at the specified location and shifts all the items after the provided
        /// index up by one.
        /// </summary>
        /// <param name="index">The index to remove from the list</param>
        public void RemoveAt(long index)
        {
            if (Count <= this.ExpandListSize)
            {
                int iArray32MemberIndex = Convert.ToInt32(index); // 0 based
                shortList.RemoveAt(iArray32MemberIndex);
            }
            else // handle the case of multiple arrays
            {
                // find out which array it is in
                int arrayIndex = (int) (index / this.ExpandListSize);
                List<T> arr = expandedList[arrayIndex];

                // find out index into this array
                int iArray32MemberIndex = (int) (index % this.ExpandListSize);
                arr.RemoveAt(iArray32MemberIndex);

                // now shift members of the array back one
                int iArray32TotalIndex = (int) (Count / this.ExpandListSize);
                for (int i = arrayIndex + 1; i < iArray32TotalIndex; i++)
                {
                    List<T> arr1 = expandedList[i - 1];
                    List<T> arr2 = expandedList[i];

                    arr1.Add(arr2[this.ExpandListSize - 1]);
                    arr2.RemoveAt(0);
                }
            }
            --Count;
        }

        #endregion

        #region IEnumerable<object> Implementation

        /// <summary>
        /// Returns a generic enumerator for enumeration of this LongList
        /// </summary>
        /// <returns>Enumerator for LongList</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new LongListEnumerator<T>(this);
        }

        /// <summary>
        /// Returns an enumerator for enumeration of this LongList
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public class LongListEnumerator<TEt> : IEnumerator<TEt>
        {
            #region Member Variables

            /// <summary>
            /// The index into the list of the item that is the current item
            /// </summary>
            private long index;

            /// <summary>
            /// The current list that we're iterating over.
            /// </summary>
            private readonly LongList<TEt> localList;

            #endregion

            /// <summary>
            /// Constructs a new enumerator for a given LongList
            /// </summary>
            /// <param name="list">The list to enumerate</param>
            public LongListEnumerator(LongList<TEt> list)
            {
                localList = list;
                index = 0;
                Current = default(TEt);
            }

            #region IEnumerator Implementation

            /// <summary>
            /// Returns the current item in the enumeration
            /// </summary>
            public TEt Current { get; private set; }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            /// <summary>
            /// Moves to the next item in the list we're iterating over
            /// </summary>
            /// <returns>Whether or not the move was successful</returns>
            public bool MoveNext()
            {
                if (index < localList.Count)
                {
                    Current = localList[index];
                    index++;
                    return true;
                }
                Current = default(TEt);
                return false;
            }

            /// <summary>
            /// Resets the enumeration
            /// </summary>
            public void Reset()
            {
                index = 0;
                Current = default(TEt);
            }

            /// <summary>
            /// Disposal method. Does nothing.
            /// </summary>
            public void Dispose()
            {
            }

            #endregion
        }
    }
}

