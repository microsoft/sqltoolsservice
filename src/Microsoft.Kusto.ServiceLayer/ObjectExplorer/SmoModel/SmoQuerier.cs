//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Extensibility;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// A <see cref="DataSourceQuerier"/> handles SMO queries for one or more SMO object types.
    /// The <see cref="SupportedObjectTypes"/> property defines which types can be queried.
    /// 
    /// To query multiple 
    /// </summary>
    public abstract class DataSourceQuerier : IComposableService
    {
        private static object lockObject = new object();

        internal IMultiServiceProvider ServiceProvider
        {
            get;
            private set;
        }

        public void SetServiceProvider(IMultiServiceProvider provider)
        {
            ServiceProvider = provider;
        }

        /// <summary>
        /// Indicates which platforms the querier is valid for
        /// </summary>
        public virtual ValidForFlag ValidFor
        {
            get
            {
                return ValidForFlag.All;
            }
        }
    }
}
