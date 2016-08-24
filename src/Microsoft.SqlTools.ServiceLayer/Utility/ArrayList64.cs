using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public class ArrayList64
    {
        private List<object> shortList;
        public long Count { get; private set; }
        private List<List<object>> expandedList;

        public ArrayList64()
        {
            shortList = new List<object>();
            Count = 0;
        }

        public long Add(object val)
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
                    expandedList = new List<List<object>>();

                    // immediately add 0th array
                    expandedList.Add(shortList);
                }

                int arrayIndex = (int)(Count/int.MaxValue); // 0 based

                List<object> arr;
                if (expandedList.Count <= arrayIndex) // need to make a new array
                {
                    arr = new List<object>();
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
                List<object> arr = expandedList[arrayIndex];

                // find out index into this array
                int iArray32MemberIndex = (int) (index%int.MaxValue);
                arr.RemoveAt(iArray32MemberIndex);

                // now shift members of the array back one
                int iArray32TotalIndex = (int) (Count/Int32.MaxValue);
                for (int i = arrayIndex + 1; i < iArray32TotalIndex; i++)
                {
                    List<object> arr1 = expandedList[i - 1];
                    List<object> arr2 = expandedList[i];

                    arr1.Add(arr2[int.MaxValue - 1]);
                    arr2.RemoveAt(0);
                }
            }
            --Count;
        }

        public object GetItem(long index)
        {
            object val = null;

            if (Count <= Int32.MaxValue)
            {
                int i32Index = Convert.ToInt32(index);
                val = shortList[i32Index];
            }
            else
            {
                int iArray32Index = (int) (Count/int.MaxValue);
                if (expandedList.Count > iArray32Index)
                {
                    List<object> arr = expandedList[iArray32Index];

                    int i32Index = (int) (Count%Int32.MaxValue);
                    if (arr.Count > i32Index)
                    {
                        val = arr[i32Index];
                    }
                }
            }
            return val;
        }
    }
}

