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
    internal sealed class ResettableLazy<T>
    {
        private volatile Lazy<T> _lazy;
        private Func<T> _factory;

        public ResettableLazy(Func<T> factory)
        {
            _factory = factory;
            _lazy = new Lazy<T>(factory);
        }

        public T Value => _lazy.Value;

        /// <summary>Resets to the original factory; next <see cref="Value"/> access re-evaluates.</summary>
        public void Reset()
        {
            _lazy = new Lazy<T>(_factory);
        }

        /// <summary>Switches to a new factory and resets; next <see cref="Value"/> uses the new factory.</summary>
        public void Reset(Func<T> newFactory)
        {
            _factory = newFactory;
            _lazy = new Lazy<T>(newFactory);
        }
    }
}
