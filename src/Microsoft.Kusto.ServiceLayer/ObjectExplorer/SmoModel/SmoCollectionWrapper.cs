//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.OEModel
{
    /// <summary>
    /// Wrapper to convert non-generic Smo enumerables to generic enumerable types for easier use in 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SmoCollectionWrapper<T> : IEnumerable<T>
        where T : SqlSmoObject
    {
        private SmoCollectionBase collection;

        /// <summary>
        /// Constructor which accepts a <see cref="SmoCollectionBase"/> containing the objects
        /// to wrap 
        /// </summary>
        /// <param name="collection"><see cref="SmoCollectionBase"/> or null if none were set</param>
        public SmoCollectionWrapper(SmoCollectionBase collection)
        {
            this.collection = collection;
        }

        /// <summary>
        /// <see cref="IEnumerable{T}.GetEnumerator"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (collection == null)
            {
                yield break;
            }
            foreach(Object obj in collection)
            {
                yield return (T)obj;
            }
        }

        /// <summary>
        /// <see cref="IEnumerable.GetEnumerator"/>
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return collection?.GetEnumerator();
        }
    }
}
