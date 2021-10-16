//------------------------------------------------------------------------------
// <copyright file="Selection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace Microsoft.Data.Tools.Design.Core.Context
{
    /// <summary>
    /// The Selection class defines a selection of T.  Selections
    /// consist of zero or more items.  The first item in a selection
    /// is defined as the "primary" selection, which is used when
    /// one object in a group must be used as a key.
    /// </summary>
    public sealed class Selection<T, Owner> : ContextItem, IDisposable
    {

        private IList<T> _selectedObjects;
        private T _container;

        /// <summary>
        /// Creates an empty Selection object.
        /// </summary>.
        public Selection()
        {
            _selectedObjects = new T[0];

        }

        /// <summary>
        /// Creates a collection object comprising the given
        /// selected objects.  The first object in the enumeration
        /// is considered the "primary" selection.
        /// </summary>
        /// <param name="selectedObjects">An enumeration of objects that should be selected.</param>
        /// <exception cref="ArgumentNullException">If selectedObjects is null.</exception>
        public Selection(IEnumerable<T> selectedObjects, T container)
        {
            if (selectedObjects == null)
            {
                throw new ArgumentNullException("selectedObjects");
            }

            _container = container;

            HashSet<T> selection = new HashSet<T>();
            List<T> orderedSelection = new List<T>();
            foreach (T info in selectedObjects)
            {
                if (!selection.Contains(info) && IsValid(info))
                {
                    selection.Add(info);
                    orderedSelection.Add(info);
                }
            }

            _selectedObjects = orderedSelection;
        }

        public void Dispose()
        {
            _container = default(T);
            _selectedObjects = null;
        }

        internal static bool IsValid(T info)
        {
            return info != null && IsAllowed(info);
        }

        public T Container
        {
            get { return _container; }
            private set { _container = value; }
        }

        /// <summary>
        /// The primary selection.  Some functions require a "key"
        /// element.  For example, an "align lefts" command needs
        /// to know which element's "left" to align to.
        /// </summary>
        public T PrimarySelection
        {
            get
            {
                foreach (T obj in _selectedObjects)
                {
                    return obj;
                }
                return default(T);
            }
        }

        /// <summary>
        /// The enumeration of selected objects.
        /// </summary>
        public IEnumerable<T> SelectedObjects
        {
            get
            {
                return _selectedObjects;
            }
        }

        /// <summary>
        /// The number of objects that are currently selected into
        /// this selection.
        /// </summary>
        public int SelectionCount
        {
            get { return _selectedObjects.Count; }
        }

        /// <summary>
        /// Override of ContextItem's ItemType
        /// property.  The ItemType of Selection is
        /// always "typeof(Selection)".
        /// </summary>
        public override Type ItemType
        {
            get
            {
                return typeof(Selection<T, Owner>);
            }
        }

        #region selection filtering

        private static Predicate<T> _selectionFilter;

        public static void RegisterSelectionFilter(Predicate<T> selectionFilter)
        {
            _selectionFilter = selectionFilter;
        }

        private static bool IsAllowed(T info)
        {
            if (_selectionFilter != null)
            {
                return _selectionFilter.Invoke(info);
            }

            return true;
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Clears the selection contained in the editing context.
        /// </summary>
        /// <param name="context">The editing context to apply this selection change to.</param>
        /// <exception cref="ArgumentNullException">If context is null.</exception>
        public static void Clear(EditingContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            ReplaceIfDifferent(context, new T[] { });
        }


        /// <summary>
        /// Selection helper method.  This sets itemToSelect into the selection.
        /// Any existing items are deselected.
        /// </summary>
        /// <param name="context">The editing context to apply this selection change to.</param>
        /// <param name="itemToSelect">The item to select.</param>
        /// <returns>A Selection object that contains the new selection.</returns>
        /// <exception cref="ArgumentNullException">If context or itemToSelect is null.</exception>
        public static Selection<T, Owner> SelectOnly(EditingContext context, T itemToSelect)
        {

            if (context == null) throw new ArgumentNullException("context");
            if (itemToSelect == null) throw new ArgumentNullException("itemToSelect");

            // Check to see if only this object is selected.  If so, bail.
            Selection<T, Owner> existing = context.Items.GetValue<Selection<T, Owner>>();
            if ((object)existing.PrimarySelection == (object)itemToSelect)
            {
                IEnumerator<T> en = existing.SelectedObjects.GetEnumerator();
                en.MoveNext();
                if (!en.MoveNext())
                {
                    return existing;
                }
            }

            Selection<T, Owner> selection = new Selection<T, Owner>(new T [] { itemToSelect }, existing.Container);
            context.Items.SetValue(selection);
            return selection;
        }

        /// <summary>
        /// Selection helper method.  This replaces the current selection with a
        /// new active set containing only the given items.
        /// </summary>
        /// <param name="context">The editing context to apply this active set change to.</param>
        /// <param name="itemToSelect">The items that will become the active set.</param>
        /// <returns>An Selection object that contains the new active set.</returns>
        /// <exception cref="ArgumentNullException">If context or item is null.</exception>
        public static Selection<T, Owner> Replace(EditingContext context, IEnumerable<T> itemsToSelect)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (itemsToSelect == null) throw new ArgumentNullException("itemsToSelect");

            return ReplaceIfDifferent(context, itemsToSelect);
        }

        private static Selection<T, Owner> ReplaceIfDifferent(EditingContext context, IEnumerable<T> itemsToSelect)
        {
            Selection<T, Owner> existing = context.Items.GetValue<Selection<T, Owner>>();

            IEnumerator<T> en1 = existing.SelectedObjects.GetEnumerator();
            IEnumerator<T> en2 = itemsToSelect.GetEnumerator();

            bool changed = false;

            bool m1;
            bool m2;

            while (true)
            {
                m1 = en1.MoveNext();
                m2 = en2.MoveNext();

                while (m2)
                {
                    if (IsAllowed(en2.Current))
                    {
                        break;
                    }
                    else
                    {
                        m2 = en2.MoveNext();
                    }
                }

                if (m1 == m2)
                {
                    if (!m1)
                    {
                        break; // reached end of both collections
                    }
                    else
                    {
                        if ((object)en1.Current != (object)en2.Current)
                        {
                            changed = true;
                            break;
                        }
                    }
                }
                else
                {
                    changed = true; // one collection ended before the other
                    break;
                }
            }

            if (changed)
            {
                Selection<T, Owner> selection = new Selection<T, Owner>(itemsToSelect, existing.Container);
                context.Items.SetValue(selection);
                return selection;
            }
            else
            {
                return existing;
            }
        }

        /// <summary>
        /// Helper method that subscribes to selection change events.
        /// </summary>
        /// <param name="context">The editing context to listen to.</param>
        /// <param name="handler">The handler to be invoked when the selection changes.</param>
        public static void Subscribe(EditingContext context, SubscribeContextCallback<Selection<T, Owner>> handler)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (handler == null) throw new ArgumentNullException("handler");
            context.Items.Subscribe<Selection<T, Owner>>(handler);
        }

        /// <summary>
        /// Selection helper method.  This takes the existing selection in the
        /// context and creates a new selection that contains the toggled
        /// state of the item.  If the item is to be
        /// added to the selection, it is added as the primary selection.
        /// </summary>
        /// <param name="context">The editing context to apply this selection change to.</param>
        /// <param name="itemToToggle">The item to toggle selection for.</param>
        /// <returns>A Selection object that contains the new selection.</returns>
        /// <exception cref="ArgumentNullException">If context or itemToToggle is null.</exception>
        public static Selection<T, Owner> Toggle(EditingContext context, T itemToToggle)
        {

            if (context == null) throw new ArgumentNullException("context");
            if (itemToToggle == null) throw new ArgumentNullException("itemToToggle");

            Selection<T, Owner> existing = context.Items.GetValue<Selection<T, Owner>>();

            // Is the item already in the selection?  If so, remove it.
            // If not, add it to the beginning.

            List<T> list = new List<T>(existing.SelectedObjects);
            if (list.Contains(itemToToggle))
            {
                list.Remove(itemToToggle);
            }
            else
            {
                list.Insert(0, itemToToggle);
            }

            Selection<T, Owner> selection = new Selection<T, Owner>(list, existing.Container);
            context.Items.SetValue(selection);
            return selection;
        }

        public static Selection<T, Owner> AddRemove(EditingContext context, IEnumerable<T> added, IEnumerable<T> removed)
        {
            if (context == null) throw new ArgumentNullException("context");

            Selection<T, Owner> existing = context.Items.GetValue<Selection<T, Owner>>();

            IEnumerable<T> selectedObjects = existing.SelectedObjects;
            if (removed != null)
            {
                selectedObjects = selectedObjects.Except(removed);
            }
            if (added != null)
            {
                selectedObjects = selectedObjects.Concat(added).Distinct();
            }

            return ReplaceIfDifferent(context, selectedObjects);
        }

        /// <summary>
        /// Set the container object for the selection
        /// </summary>
        /// <param name="context"></param>
        /// <param name="container"></param>
        public static void SetContainer(EditingContext context, T container)
        {
            if (context == null) throw new ArgumentNullException("context");

            Selection<T, Owner> existing = context.Items.GetValue<Selection<T, Owner>>();
            existing.Container = container;

        }

        /// <summary>
        /// Set the primary selection to be the specified item
        /// </summary>
        /// <param name="context"></param>
        /// <param name="item"></param>
        /// <returns>false if specified item doesn't exist in the selected objects collection</returns>
        public static bool SetPrimarySelection(EditingContext context, T item)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (item == null) throw new ArgumentNullException("item");

            Selection<T, Owner> existing = context.Items.GetValue<Selection<T, Owner>>();
            List<T> list = new List<T>(existing.SelectedObjects);
            int index = list.IndexOf(item);
            if (index >= 0)
            {
                if (index > 0)
                {
                    list.RemoveAt(index);
                    list.Insert(0, item);
                    Selection<T, Owner> selection = new Selection<T, Owner>(list, existing.Container);
                    context.Items.SetValue(selection);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Helper method that removes a previously added selection change event.
        /// </summary>
        /// <param name="context">The editing context to listen to.</param>
        /// <param name="handler">The handler to be invoked when the selection changes.</param>
        public static void Unsubscribe(EditingContext context, SubscribeContextCallback<Selection<T, Owner>> handler)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (handler == null) throw new ArgumentNullException("handler");
            context.Items.Unsubscribe<Selection<T, Owner>>(handler);
        }

        public static T GetPrimarySelection(EditingContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            Selection<T, Owner> existing = context.Items.GetValue<Selection<T, Owner>>();
            return existing.PrimarySelection;
        }

        public static IEnumerable<T> GetSelectedObjects(EditingContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            Selection<T, Owner> existing = context.Items.GetValue<Selection<T, Owner>>();
            return existing.SelectedObjects;
        }

        #endregion
    }
}
