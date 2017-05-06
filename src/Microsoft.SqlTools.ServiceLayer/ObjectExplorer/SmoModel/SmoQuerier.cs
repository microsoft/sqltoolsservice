//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Tools.DataSets;
using Microsoft.SqlServer.Management.Sdk.Sfc;
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
        public abstract IEnumerable<SqlSmoObject> Query(SmoQueryContext context, string filter);

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
        /// Convert the data to data reader is possible
        /// </summary>
        protected IDataReader GetDataReader(object data)
        {
            IDataReader reader = null;
            if (data is IDataReader)
            {
               
                reader = data as IDataReader;
            }
            else if(data is Data.Tools.DataSets.DataTable)
            {
                reader = ((Data.Tools.DataSets.DataTable)data).CreateDataReader();
            }
           
            else if (data is DataSet)
            {
                reader = ((DataSet)data).Tables[0].CreateDataReader();
            }

            return reader;
        }

        /// <summary>
        /// Mthod used to do custom filtering on smo objects if cannot be implemented using the filters
        /// </summary>
        protected virtual bool PassesFinalFilters(SqlSmoObject parent, SqlSmoObject smoObject)
        {
            return true;
        }

        /// <summary>
        /// Gets the urn from the enumResult 
        /// </summary>
        protected HashSet<string> GetUrns(EnumResult enumResult)
        {
            HashSet<string> urns = null;
            if (enumResult != null && enumResult.Data != null)
            {
                urns = new HashSet<string>();
                IDataReader reader = GetDataReader(enumResult.Data);
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        urns.Add(reader.GetString(0));
                    }
                }
            }

            return urns;
        }
    }
    
}
