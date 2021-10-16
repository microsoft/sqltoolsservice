//------------------------------------------------------------------------------
// <copyright file="ContextItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Data.Tools.Design.Core.Context {

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The ContextItem class is the base class from which all context items must derive.
    /// </summary>
    public abstract class ContextItem {

        /// <summary>
        /// Creates a new ContextItem.
        /// </summary>
        protected ContextItem() {
        }

        /// <summary>
        /// Returns the item type for this editing context item.  Context items are 
        /// considered unique based on their item type.  By using ItemType to identify 
        /// a type of context item we allow several derived versions of context items to 
        /// be cataloged under the same key in the editing context.
        /// </summary>
        /// <value></value>
        public abstract Type ItemType { get; }

        /// <summary>
        /// This method is called on a context item before it is stored in the context item
        /// manager.  The previous item in the context item manager is passed.
        /// </summary>
        /// <param name="context">The editing context that is making this change.</param>
        /// <param name="previousItem">The previously active item in the context.  Because items must have default constructors a default item will be fabricated if an item is first passed into the context.</param>
        /// <returns></returns>
        protected virtual void OnItemChanged(EditingContext context, ContextItem previousItem) {
        }

        //
        // Internal API that calls OnItemChanged.  This is invoked from the
        // abstract ContextItemCollection class so deriving classes can still
        // invoke it.
        //
        internal void InvokeOnItemChanged(EditingContext context, ContextItem previousItem) {
            OnItemChanged(context, previousItem);
        }

        /// <summary>
        /// Indicates if the current ContextItem can be replaced in the EditingContext
        /// </summary>
        internal virtual bool CanBeReplaced { get { return true; } }
    }

    public class ContextItem<T> : ContextItem
    {
        public ContextItem()
        {

        }

        public ContextItem(T obj)
        {
            _object = obj;
        }

        T _object;

        public T Object { get { return _object; } }

        public override Type ItemType
        {
            get { return typeof(ContextItem<T>); }
        }
    }
}
