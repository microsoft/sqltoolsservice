//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Wrapper to convert non-generic Smo enumerables to generic enumerable types for easier use in 
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TParent"></typeparam>
    public class SmoCollectionWrapper<TObject, TParent> : IEnumerable<TObject>
        where TObject : SqlSmoObject
        where TParent : SqlSmoObject
    {
        private SmoCollectionBase<TObject, TParent> collection;

        /// <summary>
        /// Constructor which accepts a <see cref="SmoCollectionBase"/> containing the objects
        /// to wrap 
        /// </summary>
        /// <param name="collection"><see cref="SmoCollectionBase"/> or null if none were set</param>
        public SmoCollectionWrapper(SmoCollectionBase<TObject, TParent> collection)
        {
            this.collection = collection;
        }

        /// <summary>
        /// <see cref="IEnumerable{T}.GetEnumerator"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TObject> GetEnumerator()
        {
            if (collection == null)
            {
                yield break;
            }
            foreach(Object obj in collection)
            {
                yield return (TObject)obj;
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
