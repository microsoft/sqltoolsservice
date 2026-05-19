//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    /// <summary>
    /// A <see cref="Lazy{T}"/> whose underlying factory can be reset so that the next
    /// access re-evaluates. Used to replace stale table wrappers after an incremental
    /// DacFx model update without rebuilding the entire schema collection.
    /// </summary>
    /// <remarks>
    /// Thread-safety: <see cref="Value"/> is safe to call concurrently with <see cref="Reset()"/>
    /// or <see cref="Reset(Func{T})"/>. The lock ensures that <c>_factory</c> and <c>_lazy</c>
    /// are always updated as an atomic pair so no reader can observe a mismatched combination.
    /// </remarks>
    internal sealed class ResettableLazy<T>
    {
        private Lazy<T> _lazy;
        private Func<T> _factory;
        private readonly object _lock = new object();

        public ResettableLazy(Func<T> factory)
        {
            _factory = factory;
            _lazy = new Lazy<T>(factory);
        }

        public T Value
        {
            get
            {
                // Read the current Lazy<T> snapshot outside the lock; Lazy<T> itself is
                // thread-safe for concurrent Value reads.
                Lazy<T> lazy;
                lock (_lock) { lazy = _lazy; }
                return lazy.Value;
            }
        }

        /// <summary>Resets to the original factory; next <see cref="Value"/> access re-evaluates.</summary>
        public void Reset()
        {
            lock (_lock)
            {
                _lazy = new Lazy<T>(_factory);
            }
        }

        /// <summary>Switches to a new factory and resets; next <see cref="Value"/> uses the new factory.</summary>
        public void Reset(Func<T> newFactory)
        {
            lock (_lock)
            {
                _factory = newFactory;
                _lazy = new Lazy<T>(newFactory);
            }
        }
    }
}
