//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Tools.DataSets;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Utility;

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
        private static object lockObject = new object();
        
        /// <summary>
        /// Queries SMO for a collection of objects using the <see cref="SmoQueryContext"/> 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract IEnumerable<SqlSmoObject> Query(SmoQueryContext context, string filter, bool refresh);

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

        protected HashSet<string> GetUrns(SmoQueryContext context, SqlSmoObject smoObject, string filter, string objectName)
        {
            HashSet<string> urns = null;
            string urn = string.Empty;
            try
            {
                string parentUrn = smoObject.Urn != null ? smoObject.Urn.Value : string.Empty;
                urn = parentUrn != null ? $"{parentUrn.ToString()}/{objectName}" + filter : string.Empty;

                if (!string.IsNullOrEmpty(urn))
                {
                    Enumerator en = new Enumerator();
                    Request request = new Request(new Urn(urn));
                    ServerConnection serverConnection = new ServerConnection(context.Server.ConnectionContext.SqlConnectionObject);
                    if (!serverConnection.IsOpen)
                    {
                        serverConnection.Connect();
                    }
                    EnumResult result = en.Process(serverConnection, request);

                    urns = GetUrns(result);
                }
            }
            catch (Exception ex)
            {
                string error = string.Format(CultureInfo.InvariantCulture, "Failed getting urns. error:{0} inner:{1} stacktrace:{2}",
                 ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                Logger.Write(LogLevel.Error, error);
                throw ex;
            }

            return urns;
        }

        /// <summary>
        /// Gets the urn from the enumResult 
        /// </summary>
        protected HashSet<string> GetUrns(EnumResult enumResult)
        {
            try
            {
                HashSet<string> urns = null;
                if (enumResult != null && enumResult.Data != null)
                {
                    urns = new HashSet<string>();
                    using (IDataReader reader = GetDataReader(enumResult.Data))
                    {
                        if (reader != null)
                        {
                            while (reader.Read())
                            {
                                urns.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                return urns;
            }
            catch(Exception ex)
            {
                string error = string.Format(CultureInfo.InvariantCulture, "Failed getting urns. error:{0} inner:{1} stacktrace:{2}",
                  ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                Logger.Write(LogLevel.Error, error);
            }

            return null;
        }

        protected IEnumerable<T> GetSmoCollectionResult<T>(HashSet<string> urns, SmoCollectionBase retValue, SqlSmoObject parent) where T : SqlSmoObject
        {
            if (urns != null)
            {
                return new SmoCollectionWrapper<T>(retValue).Where(c => PassesFinalFilters(parent, c) && urns.Contains(c.Urn));
            }
            else
            {
                return new SmoCollectionWrapper<T>(retValue).Where(c => PassesFinalFilters(parent, c));
            }
        }
    }
    
}
