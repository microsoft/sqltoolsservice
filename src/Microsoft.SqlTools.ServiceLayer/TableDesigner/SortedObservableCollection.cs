// <copyright file="SortedObservableCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Observable collection that keeps its items in sorted order
///    --- copied from VS sources
///    --- \vset\QTools\TestManagement\ActivityRuntime\Collections\SortedObservableCollection.cs
// </summary>

namespace Microsoft.Data.Tools.Design.Core.Collections
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;

    #endregion

    /// <summary>
    /// Observable collection that keeps its items in sorted order
    /// </summary>
    /// <typeparam name="T">Type of each item</typeparam>
    internal class SortedObservableCollection<T> : ObservableCollection<T>
    {
        #region Fields

        /// <summary>
        /// Comparison method used to keep the items in sorted order
        /// </summary>
        private Comparison<T> m_compare;

        #endregion

        #region Constructors and initialization

        /// <summary>
        /// Initializes base class using its default constructor
        /// </summary>
        /// <param name="comparer">Comparer used to keep the items in sorted order</param>
        public SortedObservableCollection(IComparer<T> comparer)
        {
            Initialize(comparer);
        }

        /// <summary>
        /// Initializes base class using its default constructor
        /// </summary>
        /// <param name="compare">Comparison method used to keep the items in sorted order</param>
        public SortedObservableCollection(Comparison<T> compare)
        {
            Initialize(compare);
        }

        /// <summary>
        /// Initializes the base class, passing in the specified list
        /// </summary>
        /// <param name="comparer">Comparer used to keep the items in sorted order</param>
        /// <param name="list">The list from which the elements are copied</param>
        public SortedObservableCollection(IComparer<T> comparer, List<T> list)
        {
            Initialize(comparer);
            Initialize(list);
        }

        /// <summary>
        /// Initializes the base class, passing in the specified list
        /// </summary>
        /// <param name="compare">Comparison method used to keep the items in sorted order</param>
        /// <param name="list">The list from which the elements are copied</param>
        public SortedObservableCollection(Comparison<T> compare, List<T> list)
        {
            Initialize(compare);
            Initialize(list);
        }

        /// <summary>
        /// Saves the comparison method
        /// </summary>
        /// <param name="comparer">Comparer used to keep the items in sorted order</param>
        private void Initialize(IComparer<T> comparer)
        {
            if (comparer == null)
            {
                Debug.Fail("'comparer' is null");
                throw new ArgumentNullException("comparer");
            }

            m_compare = comparer.Compare;
        }

        /// <summary>
        /// Saves the comparison method
        /// </summary>
        /// <param name="compare">Comparison method used to keep the items in sorted order</param>
        private void Initialize(Comparison<T> compare)
        {
            if (compare == null)
            {
                Debug.Fail("'compare' is null");
                throw new ArgumentNullException("compare");
            }

            m_compare = compare;
        }

        /// <summary>
        /// Sorts the list and copies the elements to this collection
        /// </summary>
        /// <param name="list">The list from which the elements are copied</param>
        private void Initialize(List<T> list)
        {
            if (list == null)
            {
                Debug.Fail("'list' is null");
                throw new ArgumentNullException("list");
            }

            List<T> listCopy = new List<T>(list.Count);
            listCopy.AddRange(list);
            listCopy.Sort(m_compare);
            foreach (T item in listCopy)
            {
                Add(item);
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Inserts an item into the list in an appropriate location, such that the list remains sorted. Uses the insertion
        /// algorithm used in insertion sort.
        /// </summary>
        /// <param name="index">
        /// The index at which to insert the item. This parameter is ignored, since this collection keeps the items sorted.
        /// </param>
        /// <param name="item">The item to insert</param>
        protected override void InsertItem(int index, T item)
        {
            // Add the item to the end of the list, and handle capacity increases, etc.
            base.InsertItem(Count, item);

            // Swap items from the end down the list until the list becomes sorted again. Use the Items property, as it gives
            // direct access to the list and changes there don't cause the CollectionChanged event to be raised. We can raise
            // the CollectionChanged event once at the end.
            int i;
            for (i = Count - 1; i >= 1 && m_compare(Items[i - 1], Items[i]) > 0; --i)
            {
                T temp = Items[i - 1];
                Items[i - 1] = Items[i];
                Items[i] = temp;
            }

            // Raise the CollectionChanged event again if we moved the new item
            if (i != Count - 1)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, i, Count - 1));
            }
        }

        #endregion
    }
}
