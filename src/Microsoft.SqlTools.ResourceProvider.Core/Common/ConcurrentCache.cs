//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    internal class ConcurrentCache<T>
    {
        private readonly Dictionary<string, T> _cache = new Dictionary<string, T>();
        private readonly ReaderWriterLock _readerWriterLock = new ReaderWriterLock();
        private readonly TimeSpan _timeout = TimeSpan.FromHours(1);

        public void ClearCache(IEnumerable<string> keys)
        {
            Exception exception;
            new AutoLock(_readerWriterLock, true, _timeout, () =>
            {
                {
                    foreach (var key in keys)
                    {
                        if (_cache.ContainsKey(key))
                        {
                            _cache.Remove(key);
                        }
                    }
                }
            }, out exception);
            if (exception != null)
            {
                throw exception;
            }
        }
        
        public T Get(string key)
        {
            T result = default(T);
            Exception exception;
            new AutoLock(_readerWriterLock, false, _timeout, () =>
            {
                if (_cache.ContainsKey(key))
                {
                    result = _cache[key];
                }
            }, out exception);
            if (exception != null)
            {
                throw exception;
            }

            return result;
        }

        public T UpdateCache(string key, T newValue)
        {
            T result = newValue;

            Exception exception;
            new AutoLock(_readerWriterLock, true, _timeout, () =>
            {
                bool isDefined = _cache.ContainsKey(key);
                if (!isDefined)
                {
                    _cache.Add(key, newValue);
                }
                else
                {
                    result = _cache[key];
                }
            }, out exception);
            if (exception != null)
            {
                throw exception;
            }

            return result;
        }
    }
}
