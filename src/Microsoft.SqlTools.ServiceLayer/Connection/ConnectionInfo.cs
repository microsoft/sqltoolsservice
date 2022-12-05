//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Information pertaining to a unique connection instance.
    /// </summary>
    public class ConnectionInfo
    {
        private static readonly DateTime UnixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectionInfo(ISqlConnectionFactory factory, string ownerUri, ConnectionDetails details)
        {
            Factory = factory;
            OwnerUri = ownerUri;
            ConnectionDetails = details;
            ConnectionId = Guid.NewGuid();
            IntellisenseMetrics = new InteractionMetrics<double>(new int[] { 50, 100, 200, 500, 1000, 2000 });
        }

        /// <summary>
        /// Unique Id, helpful to identify a connection info object
        /// </summary>
        public Guid ConnectionId { get; private set; }

        /// <summary>
        /// URI identifying the owner/user of the connection. Could be a file, service, resource, etc.
        /// </summary>
        public string OwnerUri { get; set; }

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
        internal readonly ConcurrentDictionary<string, DbConnection> ConnectionTypeToConnectionMap =
            new ConcurrentDictionary<string, DbConnection>();

        /// <summary>
        /// Intellisense Metrics
        /// </summary>
        public InteractionMetrics<double> IntellisenseMetrics { get; private set; }

        /// <summary>
        /// Returns true if the db connection is to any cloud instance
        /// </summary>
        public bool IsCloud { get; set; }

        /// <summary>
        /// Returns true if the db connection is to a SQL db instance
        /// </summary>
        public bool IsSqlDb { get; set; }

        /// <summary>
        /// Returns true if the sql connection is to a DW instance
        /// </summary>
        public bool IsSqlDW { get; set; }

        /// <summary>
        /// Returns true if Authentication mode is AzureMFA, determines if Access token is in use.
        /// </summary>
        public bool IsAzureAuth { get; set; }

        /// <summary>
        /// Returns the connection Engine Edition
        /// </summary>
        public DatabaseEngineEdition EngineEdition { get; set; }

        /// <summary>
        /// Returns the major version number of the db we are connected to 
        /// </summary>
        public int MajorVersion { get; set; }

        /// <summary>
        /// All DbConnection instances held by this ConnectionInfo
        /// </summary>
        public ICollection<DbConnection> AllConnections
        {
            get
            {
                return ConnectionTypeToConnectionMap.Values;
            }
        }

        /// <summary>
        /// All connection type strings held by this ConnectionInfo
        /// </summary>
        /// <returns></returns>
        public ICollection<string> AllConnectionTypes
        {
            get
            {
                return ConnectionTypeToConnectionMap.Keys;
            }
        }

        public bool HasConnectionType(string connectionType)
        {
            connectionType = connectionType ?? ConnectionType.Default;
            return ConnectionTypeToConnectionMap.ContainsKey(connectionType);
        }

        /// <summary>
        /// The count of DbConnectioninstances held by this ConnectionInfo 
        /// </summary>
        public int CountConnections
        {
            get
            {
                return ConnectionTypeToConnectionMap.Count;
            }
        }

        /// <summary>
        /// Try to get the DbConnection associated with the given connection type string. 
        /// </summary>
        /// <returns>true if a connection with type connectionType was located and out connection was set, 
        /// false otherwise </returns>
        /// <exception cref="ArgumentException">Thrown when connectionType is null or empty</exception>
        public bool TryGetConnection(string connectionType, out DbConnection connection)
        {
            Validate.IsNotNullOrEmptyString("Connection Type", connectionType);
            return ConnectionTypeToConnectionMap.TryGetValue(connectionType, out connection);
        }

        /// <summary>
        /// Adds a DbConnection to this object and associates it with the given 
        /// connection type string. If a connection already exists with an identical 
        /// connection type string, it is not overwritten. Ignores calls where connectionType = null
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when connectionType is null or empty</exception>
        public void AddConnection(string connectionType, DbConnection connection)
        {
            Validate.IsNotNullOrEmptyString("Connection Type", connectionType);
            ConnectionTypeToConnectionMap.TryAdd(connectionType, connection);
        }

        /// <summary>
        /// Removes the single DbConnection instance associated with string connectionType
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when connectionType is null or empty</exception>
        public void RemoveConnection(string connectionType)
        {
            Validate.IsNotNullOrEmptyString("Connection Type", connectionType);
            ConnectionTypeToConnectionMap.TryRemove(connectionType, out _);
        }

        /// <summary>
        /// Removes all DbConnection instances held by this object
        /// </summary>
        public void RemoveAllConnections()
        {
            foreach (var type in AllConnectionTypes)
            {
                ConnectionTypeToConnectionMap.TryRemove(type, out _);
            }
        }

        /// <summary>
        /// Updates the Auth Token and Expires On fields
        /// </summary>
        public bool TryUpdateAccessToken(SecurityToken? securityToken)
        {
            if (securityToken != null && !string.IsNullOrEmpty(securityToken.Token) && IsAzureAuth && IsAccessTokenExpired)
            {
                ConnectionDetails.AzureAccountToken = securityToken.Token;
                ConnectionDetails.ExpiresOn = securityToken.ExpiresOn;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if Access token saved in connection details is expired or about to expire in 2 minutes.
        /// </summary>
        private bool IsAccessTokenExpired
        {
            get
            {
                if (IsAzureAuth && ConnectionDetails.ExpiresOn != null && double.TryParse(ConnectionDetails.ExpiresOn.ToString(), out var expiresOn))
                {
                    DateTime dateTime = UnixEpochUtc.AddSeconds(expiresOn);

                    // Check if access token is already expired or shall expire in 2 minutes.
                    if (dateTime <= DateTime.UtcNow.AddMinutes(2))
                    {
                        Logger.Verbose("Access token found expired.");
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
