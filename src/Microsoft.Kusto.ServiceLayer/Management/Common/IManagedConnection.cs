//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;

namespace Microsoft.Kusto.ServiceLayer.Management
{

    /// <summary>
    /// Provides connection and enumerator context for a node
    /// </summary>
    public interface IManagedConnection : IDisposable
    {
        /// <summary>
        /// Connection information.
        /// </summary>
        SqlOlapConnectionInfoBase Connection
        {
            get;
        }

        /// <summary>
        /// Free any resources for this connection
        /// </summary>
        void Close();
    }

    /// <summary>
    /// interface used by the objectexplorer. Allows us to "pool" the main connection
    /// </summary>
    internal interface IManagedConnection2 : IManagedConnection
    {
    }

    /// <summary>
    /// Implementation of IManagedConnection. Allows the use of a direct or indirect connection
    /// in the object explorer that takes care of the connection.
    /// </summary>
    internal class ManagedConnection : IManagedConnection2
    {
        #region private members
        private bool connectionAddedToActiveConnections = false;
        private bool closeOnDispose = false;
        private SqlOlapConnectionInfoBase connection;
        private bool closed = false;
        #endregion

        #region Construction
        /// <summary>
        /// Create a new managed connection
        /// </summary>
        /// <param name="connection">connection wish to manage</param>
        public ManagedConnection(SqlOlapConnectionInfoBase connection)
            : this(connection, false)
        {
        }
        /// <summary>
        /// create a new managed connection.
        /// </summary>
        /// <param name="connection">connection</param>
        /// <param name="attemptToPool">true if we are going to try and reuse the
        /// connection if possible</param>
        public ManagedConnection(SqlOlapConnectionInfoBase sourceConnection, bool attemptToPool)
        {
            // parameter check
            if (sourceConnection == null)
            {
                throw new ArgumentNullException("sourceConnection");
            }

            // see if the connection can restrict access (single user mode)
            IRestrictedAccess access = sourceConnection as IRestrictedAccess;
            // see if it is cloneable
            ICloneable cloneable = sourceConnection as ICloneable;
            lock (ActiveConnections)
            {
                // if it's not single user mode then we can see if the object can be cloned
                if (access == null || access.SingleConnection == false)
                {
                    // if we are going to attempt to pool, see if the connection is in use
                    if (attemptToPool && !ActiveConnections.Contains(SharedConnectionUtil.GetConnectionKeyName(sourceConnection)))
                    {
                        // add it to the hashtable to indicate use.
                        ActiveConnections.Add(SharedConnectionUtil.GetConnectionKeyName(sourceConnection), sourceConnection);
                        this.connection = sourceConnection;
                        this.closeOnDispose = false;
                        this.connectionAddedToActiveConnections = true;
                    }
                    else if (cloneable != null)
                    {
                        this.connection = (SqlOlapConnectionInfoBase)cloneable.Clone();
                        this.closeOnDispose = true;
                    }
                    else if (sourceConnection is SqlConnectionInfoWithConnection)
                    {
                        this.connection = ((SqlConnectionInfoWithConnection)sourceConnection).Copy();
                        this.closeOnDispose = true;
                    }
                }
            }
            // if everything else has failed just use to passed in connection.
            if (this.connection == null)
            {
                this.connection = sourceConnection;
            }

            // always set the lock timeout to prevent the shell from not responding
            if (this.connection is SqlConnectionInfoWithConnection)
            {
                // set lock_timeout to 10 seconds
                ((SqlConnectionInfoWithConnection)this.connection).ServerConnection.LockTimeout = 10;
            }
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            Close();
        }
        #endregion

        #region IManagedConnection implementation
        /// <summary>
        /// Connection
        /// </summary>
        public SqlOlapConnectionInfoBase Connection
        {
            get
            {
                return this.connection;
            }
        }

        /// <summary>
        /// Close the current connection if applicable.
        /// </summary>
        public void Close()
        {
            if (this.closed)
                return;

            if (this.closeOnDispose)
            {
                IDisposable disp = this.connection as IDisposable;
                if (disp != null)
                {
                    disp.Dispose();
                }
            }
            else
            {
                // if we are not closing the connection and it is a sql connection then ensure it
                // is left in the master database.
                SqlConnectionInfoWithConnection sqlConnection = this.connection as SqlConnectionInfoWithConnection;
                if (sqlConnection != null && sqlConnection.ServerConnection.DatabaseEngineType == DatabaseEngineType.Standalone)
                {
                    try
                    {
                        sqlConnection.ServerConnection.ExecuteNonQuery("use [master]");
                    }
                    // don't error if this fails
                    catch
                    { }
                }
            }
            if (this.connectionAddedToActiveConnections)
            {
                lock (ActiveConnections)
                {
                    ActiveConnections.Remove(SharedConnectionUtil.GetConnectionKeyName(connection));
                }
            }

            this.connection = null;
            this.closed = true;
        }
        #endregion

        #region static helpers
        /// <summary>
        /// hashtable we use to keep track of actively used main connections
        /// </summary>
        private static Hashtable activeConnections = null;
        private Hashtable ActiveConnections
        {
            get
            {
                if (activeConnections == null)
                {
                    activeConnections = new Hashtable();
                }
                return activeConnections;
            }
        }
        #endregion
    }
}