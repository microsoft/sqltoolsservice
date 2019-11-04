//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Kusto;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.KustoModel
{
    /// <summary>
    /// Wrapper to convert non-generic Kusto enumerables to generic enumerable types for easier use in 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KustoCollectionWrapper<T> : IEnumerable<T>
        where T : SqlKustoObject
    {
        private KustoCollectionBase collection;

        /// <summary>
        /// Constructor which accepts a <see cref="KustoCollectionBase"/> containing the objects
        /// to wrap 
        /// </summary>
        /// <param name="collection"><see cref="KustoCollectionBase"/> or null if none were set</param>
        public KustoCollectionWrapper(KustoCollectionBase collection)
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
