using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public class LongList<T> : IEnumerable<T>
    {
        private readonly List<T> shortList;
        public long Count { get; private set; }
        private List<List<T>> expandedList;

        public LongList()
        {
            shortList = new List<T>();
            Count = 0;
        }

        public long Add(T val)
        {
            if (Count <= int.MaxValue)
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

                int arrayIndex = (int)(Count/int.MaxValue); // 0 based

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

        public void RemoveAt(long index)
        {
            if (Count <= int.MaxValue)
            {
                int iArray32MemberIndex = Convert.ToInt32(index); // 0 based
                shortList.RemoveAt(iArray32MemberIndex);
            }
            else // handle the case of multiple arrays
            {
                // find out which array it is in
                int arrayIndex = (int) (index/int.MaxValue);
                List<T> arr = expandedList[arrayIndex];

                // find out index into this array
                int iArray32MemberIndex = (int) (index%int.MaxValue);
                arr.RemoveAt(iArray32MemberIndex);

                // now shift members of the array back one
                int iArray32TotalIndex = (int) (Count/Int32.MaxValue);
                for (int i = arrayIndex + 1; i < iArray32TotalIndex; i++)
                {
                    List<T> arr1 = expandedList[i - 1];
                    List<T> arr2 = expandedList[i];

                    arr1.Add(arr2[int.MaxValue - 1]);
                    arr2.RemoveAt(0);
                }
            }
            --Count;
        }

        public T GetItem(long index)
        {
            object val = null;

            if (Count <= int.MaxValue)
            {
                int i32Index = Convert.ToInt32(index);
                val = shortList[i32Index];
            }
            else
            {
                int iArray32Index = (int) (Count/int.MaxValue);
                if (expandedList.Count > iArray32Index)
                {
                    List<T> arr = expandedList[iArray32Index];

                    int i32Index = (int) (Count%int.MaxValue);
                    if (arr.Count > i32Index)
                    {
                        val = arr[i32Index];
                    }
                }
            }
            return val;
        }

        public T this[long index]
        {
            get { return GetItem(index); }
        }

        #region IEnumerable<object> Implementation

        public IEnumerator<T> GetEnumerator()
        {
            return new LongListEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public class LongListEnumerator<TEt> : IEnumerator<TEt>
        {
            #region Properties

            /// <summary>
            /// The current list that we're iterating over.
            /// </summary>
            private LongList<TEt> List { get; set; }

            /// <summary>
            /// The index into the list of the item that is the current item
            /// </summary>
            private long CurrentIndex { get; set; }

            #endregion

            #region IEnumerator Implementation

            public LongListEnumerator(LongList<TEt> list)
            {
                List = list;
                CurrentIndex = 0;
            }

            public bool MoveNext()
            {
                CurrentIndex++;
                return CurrentIndex < List.Count;
            }

            public void Reset()
            {
                CurrentIndex = 0;
            }

            public TEt Current { get { return List[CurrentIndex]; } }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
            }

            #endregion
        }
    }
}

