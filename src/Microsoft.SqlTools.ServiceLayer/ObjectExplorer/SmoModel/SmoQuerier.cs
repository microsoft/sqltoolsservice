//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// A <see cref="SmoQuerier"/> handles SMO queries for one or more SMO object types.
    /// The <see cref="SupportedObjectTypes"/> property defines which types can be queried.
    /// 
    /// To query multiple 
    /// </summary>
    public abstract class SmoQuerier : IComposableService
    {
        public abstract Type[] SupportedObjectTypes { get;  }
        
        /// <summary>
        /// Queries SMO for a collection of objects using the <see cref="SmoQueryContext"/> 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract IEnumerable<SqlSmoObject> Query(SmoQueryContext context);

        internal IMultiServiceProvider ServiceProvider
        {
            get;
            private set;
        }

        public void SetServiceProvider(IMultiServiceProvider provider)
        {
            ServiceProvider = provider;
        }
    }
    
}
