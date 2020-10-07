//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    /// <summary>
    /// ConnectedBindingQueue class for processing online binding requests
    /// </summary>
    [Export(typeof(IConnectedBindingQueue))]
    public class ConnectedBindingQueue : BindingQueue<ConnectedBindingContext>, IConnectedBindingQueue
    {
        internal const int DefaultBindingTimeout = 500;

        /// <summary>
        /// Generate a unique key based on the ConnectionInfo object
        /// </summary>
        /// <param name="details"></param>
        internal static string GetConnectionContextKey(ConnectionDetails details)
        {            
            string key = string.Format("{0}_{1}_{2}_{3}",
                details.ServerName ?? "NULL",
                details.DatabaseName ?? "NULL",
                details.UserName ?? "NULL",
                details.AuthenticationType ?? "NULL"
            );

            if (!string.IsNullOrEmpty(details.DatabaseDisplayName))
            {
                key += "_" + details.DatabaseDisplayName;
            }

            if (!string.IsNullOrEmpty(details.GroupId))
            {
                key += "_" + details.GroupId;
            }

            return Uri.EscapeUriString(key);
        }

        public void RemoveBindingContext(ConnectionInfo connInfo)
        {
            string connectionKey = GetConnectionContextKey(connInfo.ConnectionDetails);
            if (BindingContextExists(connectionKey))
            {
                RemoveBindingContext(connectionKey);
            }
        }

        /// <summary>
        /// Use a ConnectionInfo item to create a connected binding context
        /// </summary>
        /// <param name="connInfo">Connection info used to create binding context</param>
        /// <param name="needMetadata"></param>
        /// <param name="featureName"></param>
        /// <param name="overwrite">Overwrite existing context</param>
        public string AddConnectionContext(ConnectionInfo connInfo, bool needMetadata, string featureName = null, bool overwrite = false)
        {
            if (connInfo == null)
            {
                return string.Empty;
            }

            // lookup the current binding contextna
            string connectionKey = GetConnectionContextKey(connInfo.ConnectionDetails);
            if (BindingContextExists(connectionKey))
            {
                if (overwrite)
                {
                    RemoveBindingContext(connectionKey);
                }
                else
                {
                    // no need to populate the context again since the context already exists
                    return connectionKey;
                }
            }
            IBindingContext bindingContext = GetOrCreateBindingContext(connectionKey);

            if (bindingContext.BindingLock.WaitOne())
            {
                try
                {
                    bindingContext.BindingLock.Reset();
                    connInfo.TryGetConnection(ConnectionType.ObjectExplorer, out ReliableDataSourceConnection  connection);
                    
                    if (connection == null)
                    {
                        connInfo.TryGetConnection(ConnectionType.Default, out connection);    
                    }
                    bindingContext.DataSource = connection.GetUnderlyingConnection();
                    bindingContext.BindingTimeout = DefaultBindingTimeout;
                    bindingContext.IsConnected = true;
                }
                catch (Exception)
                {
                    bindingContext.IsConnected = false;
                }       
                finally
                {
                    bindingContext.BindingLock.Set();
                }         
            }

            return connectionKey;
        }
    }
}
