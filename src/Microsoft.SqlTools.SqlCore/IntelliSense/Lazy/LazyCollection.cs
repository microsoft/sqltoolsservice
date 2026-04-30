//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

// Written from scratch (no direct SSDT equivalent).
// Provides IMetadataCollection<T> and IMetadataOrderedCollection<T> implementations backed by
// a Lazy<T[]> so that TSqlModel scans are deferred until the collection is first enumerated.

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    /// <summary>
    /// Lazy <see cref="IMetadataCollection{T}"/> that loads items on first access.
    /// </summary>
    internal sealed class LazyCollection<T> : IMetadataCollection<T>
        where T : class, IMetadataObject
    {
        private static readonly LazyCollection<T> s_empty =
            new LazyCollection<T>(Array.Empty<T>);

        public static IMetadataCollection<T> Empty => s_empty;

        private readonly Lazy<T[]> _items;

        public LazyCollection(Func<IEnumerable<T>> loader)
        {
            _items = new Lazy<T[]>(() => loader().ToArray());
        }

        public int Count => _items.Value.Length;

        public T this[string name] => _items.Value.FirstOrDefault(
            i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))!;

        public bool Contains(string name) =>
            _items.Value.Any(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

        public bool Contains(T item) => _items.Value.Contains(item);

        public IEnumerable<T> FindAll(Predicate<T> predicate) =>
            _items.Value.Where(i => predicate(i));

        public IEnumerable<T> FindAll(string name) =>
            _items.Value.Where(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

        public IMetadataCollection<IMetadataObject> AsMetadataObjectCollection =>
            new LazyCollection<IMetadataObject>(_items.Value.Cast<IMetadataObject>);

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items.Value).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.Value.GetEnumerator();
    }

    /// <summary>
    /// Lazy <see cref="IMetadataOrderedCollection{T}"/> that loads items on first access.
    /// Used for Columns (which are ordered).
    /// </summary>
    internal sealed class LazyOrderedCollection<T> : IMetadataOrderedCollection<T>
        where T : class, IMetadataObject
    {
        private static readonly LazyOrderedCollection<T> s_empty =
            new LazyOrderedCollection<T>(Array.Empty<T>);

        public static IMetadataOrderedCollection<T> Empty => s_empty;

        private readonly Lazy<T[]> _items;

        public LazyOrderedCollection(Func<IEnumerable<T>> loader)
        {
            _items = new Lazy<T[]>(() => loader().ToArray());
        }

        public int Count => _items.Value.Length;

        // Ordered index access
        public T this[int index] => _items.Value[index];

        // Name-based access (IMetadataCollection<T>)
        public T this[string name] => _items.Value.FirstOrDefault(
            i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))!;

        public bool Contains(string name) =>
            _items.Value.Any(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

        public bool Contains(T item) => _items.Value.Contains(item);

        public IEnumerable<T> FindAll(Predicate<T> predicate) =>
            _items.Value.Where(i => predicate(i));

        public IEnumerable<T> FindAll(string name) =>
            _items.Value.Where(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

        public IMetadataCollection<IMetadataObject> AsMetadataObjectCollection =>
            new LazyCollection<IMetadataObject>(_items.Value.Cast<IMetadataObject>);

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items.Value).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.Value.GetEnumerator();
    }
}
