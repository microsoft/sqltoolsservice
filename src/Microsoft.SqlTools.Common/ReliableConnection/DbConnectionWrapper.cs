//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// Wraps <see cref="IDbConnection"/> objects that could be a <see cref="SqlConnection"/> or
    /// a <see cref="ReliableSqlConnection"/>, providing common methods across both.
    /// </summary>
    public sealed class DbConnectionWrapper
    {
        private readonly IDbConnection _connection;
        private readonly bool _isReliableConnection;

        public DbConnectionWrapper(IDbConnection connection)
        {
            Validate.IsNotNull(nameof(connection), connection);
            if (connection is ReliableSqlConnection)
            {
                _isReliableConnection = true;
            }
            else if (!(connection is SqlConnection))
            {
                throw new InvalidOperationException(Resources.InvalidConnectionType);
            }
            
            _connection = connection;
        }

        public static bool IsSupportedConnection(IDbConnection connection)
        {
            return connection is ReliableSqlConnection
                || connection is SqlConnection;
        }

        public event SqlInfoMessageEventHandler InfoMessage
        {
            add
            {
                SqlConnection conn = GetAsSqlConnection();
                conn.InfoMessage += value;
            }
            remove
            {
                SqlConnection conn = GetAsSqlConnection();
                conn.InfoMessage -= value;
            }
        }

        public string DataSource
        {
            get
            {
                if (_isReliableConnection)
                {
                    return ((ReliableSqlConnection) _connection).DataSource;
                }
                return ((SqlConnection)_connection).DataSource; 
            }
        }

        public string ServerVersion
        {
            get
            {
                if (_isReliableConnection)
                {
                    return ((ReliableSqlConnection)_connection).ServerVersion;
                }
                return ((SqlConnection)_connection).ServerVersion; 
            }
        }

        /// <summary>
        /// Gets this as a SqlConnection by casting (if we know it is actually a SqlConnection)
        /// or by getting the underlying connection (if it's a ReliableSqlConnection)
        /// </summary>
        public SqlConnection GetAsSqlConnection()
        {
            if (_isReliableConnection)
            {
                return ((ReliableSqlConnection) _connection).GetUnderlyingConnection();
            }
            return (SqlConnection) _connection;
        }

        /*
        TODO - IClonable does not exist in .NET Core.
        /// <summary>
        /// Clones the connection and ensures it's opened. 
        /// If it's a SqlConnection it will clone it,
        /// and for ReliableSqlConnection it will clone the underling connection.
        /// The reason the entire ReliableSqlConnection is not cloned is that it includes
        /// several callbacks and we don't want to try and handle deciding how to clone these
        /// yet.
        /// </summary>
        public SqlConnection CloneAndOpenConnection()
        {
            SqlConnection conn = GetAsSqlConnection();
            SqlConnection clonedConn = ((ICloneable) conn).Clone() as SqlConnection;
            clonedConn.Open();
            return clonedConn;
        }
        */
    }
}
