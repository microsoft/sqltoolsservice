//------------------------------------------------------------------------------
// <copyright file="ContextLayer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Data.Tools.Design.Core.Context {

    using System;

    /// <summary>
    /// The items in the context item manager are divided into layers.  
    /// A layer may be isolated, in which it does not inherit context 
    /// from previous layers, or normal, in which it does.  Once a layer 
    /// is created new context items can be set into that layer.  When 
    /// the layer is removed, all the prior context items come back.
    /// </summary>
    public abstract class ContextLayer : IDisposable {

        /// <summary>
        /// Creates a new ContextLayer object.
        /// </summary>
        protected ContextLayer() {
        }

        /// <summary>
        /// Implements the finalization part of the IDisposable pattern by calling
        /// Dispose(false).
        /// </summary>
        ~ContextLayer() {
            Dispose(false);
        }

        /// <summary>
        /// Removes this layer from the items.
        /// </summary>
        public abstract void Remove(bool disposing = false);

        /// <summary>
        /// Disposes this layer by calling Dispose(true).
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this layer by calling Remove on it.
        /// </summary>
        /// <param name="disposing">True if the object is disposing or false if it is finalizing.</param>
        protected virtual void Dispose(bool disposing) {
            if (disposing) Remove(disposing);
        }
    }
}
