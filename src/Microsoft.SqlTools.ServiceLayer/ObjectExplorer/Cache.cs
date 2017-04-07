//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    /// <summary>
    /// A dictionary-based Cache that supports quick lookups for common objects. This is compatible with
    /// <see cref="IDictionary"/> but note that <see cref="IDictionary.Remove(object)"/> has limited support - it
    /// will effectively increase the cache size slightly since removing from the queue would be inefficient due to the
    /// need to rebuild the recently used queue.
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    internal class Cache<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> dictionary;
        private Queue<TKey> keys;
        private int capacity;

        public Cache(int capacity)
        {
            this.keys = new Queue<TKey>(capacity);
            this.capacity = capacity;
            this.dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public void Add(TKey key, TValue value)
        {
            if (dictionary.Count == capacity)
            {
                var oldestKey = keys.Dequeue();
                dictionary.Remove(oldestKey);
            }

            dictionary.Add(key, value);
            keys.Enqueue(key);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }
        
        public ICollection<TKey> Keys
        {
            get
            {
                return dictionary.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return dictionary.Values;
            }
        }

        public int Count
        {
            get
            {
                return dictionary.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                return dictionary[key];
            }

            set
            {
                if (!dictionary.ContainsKey(key))
                {
                    Add(key, value);
                }
                else
                {
                    dictionary[key] = value;
                }
            }
        }

        public bool Remove(TKey key)
        {
            // Note: This method is non-optimal. It effectively adds 1 to the cache size since the 
            return dictionary.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            keys.Clear();
            dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
        }
    }
}
