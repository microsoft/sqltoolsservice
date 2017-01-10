//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Information pertaining to a unique connection instance.
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectionInfo(ISqlConnectionFactory factory, string ownerUri, ConnectionDetails details)
        {
            Factory = factory;
            OwnerUri = ownerUri;
            ConnectionDetails = details;
            ConnectionId = Guid.NewGuid();
            IntellisenseMetrics = new InteractionMetrics<double>(new int[] {50, 100, 200, 500, 1000, 2000});
        }

        /// <summary>
        /// Unique Id, helpful to identify a connection info object
        /// </summary>
        public Guid ConnectionId { get; private set; }

        /// <summary>
        /// URI identifying the owner/user of the connection. Could be a file, service, resource, etc.
        /// </summary>
        public string OwnerUri { get; private set; }

        /// <summary>
        /// Factory used for creating the SQL connection associated with the connection info.
        /// </summary>
        public ISqlConnectionFactory Factory { get; private set; }

        /// <summary>
        /// Properties used for creating/opening the SQL connection.
        /// </summary>
        public ConnectionDetails ConnectionDetails { get; private set; }

        /// <summary>
        /// A map containing all connections to the database that are associated with 
        /// this ConnectionInfo's OwnerUri.
        /// This is internal for testing access only
        /// </summary>
        internal readonly Dictionary<string, DbConnection> ConnectionTypeToConnectionMap =
            new Dictionary<string, DbConnection>();

        /// <summary>
        /// Intellisense Metrics
        /// </summary>
        public InteractionMetrics<double> IntellisenseMetrics { get; private set; }

        /// <summary>
        /// Returns true is the db connection is to a SQL db
        /// </summary>
        public bool IsAzure { get; set; }

        /// <summary>
        /// Try to get the DbConnection associated with the given connection type string. 
        /// </summary>
        public bool TryGetConnection(string connectionType, out DbConnection connection)
        {
            if (connectionType == null)
            {
                connectionType = ConnectionType.Default;
            }

            return ConnectionTypeToConnectionMap.TryGetValue(connectionType, out connection);
        }

        /// <summary>
        /// Get a List of all DbConnection instances held by this ConnectionInfo
        /// </summary>
        public List<DbConnection> GetAllConnections()
        {
            return ConnectionTypeToConnectionMap.Values.ToList();
        }

        /// <summary>
        /// Get a list of all connection type strings held by this ConnectionInfo
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllConnectionTypes()
        {
            return ConnectionTypeToConnectionMap.Keys.ToList();
        }

        /// <summary>
        /// Gets the count of DbConnectioninstances held by this ConnectionInfo 
        /// </summary>
        public int CountConnections()
        {
            return ConnectionTypeToConnectionMap.Count;
        }

        /// <summary>
        /// Adds a DbConnection to this object and associates it with the given 
        /// connection type string. If a connection already exists with an identical 
        /// connection type string, it is overwritten. Ignores calls where connectionType = null
        /// </summary>
        public void AddConnection(string connectionType, DbConnection connection)
        {
            if (connectionType != null)
            {
                ConnectionTypeToConnectionMap.Add(connectionType, connection);
            }
        }

        /// <summary>
        /// If string connectionType is not null, removes the single DbConnection
        /// instance associated with string connectionType. If string connectionType
        /// is null, removes all DbConnection instances. 
        /// </summary>
        /// <param name="connectionType"></param>
        /// <returns>true if there are no more DbConnection instances held
        /// by this object after trying to remove the requested connection(s),  
        /// false otherwise</returns>
        public bool RemoveConnection(string connectionType)
        {
            // Remove a single DbConnection
            if (connectionType != null)
            {
                ConnectionTypeToConnectionMap.Remove(connectionType);
            }
            // Remove all DbConnections 
            else
            {
                foreach (string type in GetAllConnectionTypes())
                {
                    ConnectionTypeToConnectionMap.Remove(type);
                }
            }

            return CountConnections() == 0;
        }
    }
}
