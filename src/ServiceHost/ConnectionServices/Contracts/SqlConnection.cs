//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.ConnectionServices.Contracts
{
    /// <summary>
    /// Wrapper class that implements ISqlConnection and hosts a SqlConnection.
    /// This wrapper exists primarily for decoupling to support unit testing.
    /// </summary>
    public class SqlClientConnection : ISqlConnection
    {
        /// <summary>
        /// the underlying SQL connection
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// Creates a new instance of the SqlClientConnection with an underlying connection to the
        /// database server provided in <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">Connection string for the database to connect to</param>
        public SqlClientConnection(string connectionString)
        {
            connection = new SqlConnection(connectionString);
        }

        #region ISqlConnection Implementation

        #region Properties

        public string ConnectionString
        {
            get { return connection.ConnectionString; }
            set { connection.ConnectionString = value; }
        }

        public int ConnectionTimeout
        {
            get { return connection.ConnectionTimeout; }
        }

        public string Database
        {
            get { return connection.Database; }
        }

        public string DataSource
        {
            get { return connection.DataSource; }
        }

        public string ServerVersion
        {
            get { return connection.ServerVersion; }
        }

        public ConnectionState State
        {
            get { return connection.State; }
        }

        #endregion

        #region Public Methods

        public IDbTransaction BeginTransaction()
        {
            return connection.BeginTransaction();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return connection.BeginTransaction(il);
        }

        public void ChangeDatabase(string databaseName)
        {
            connection.ChangeDatabase(databaseName);
        }

        public void ClearPool()
        {
            if (connection != null)
            {
                SqlConnection.ClearPool(connection);
            }
        }

        public void Close()
        {
            connection.Close();
        }

        public IDbCommand CreateCommand()
        {
            return connection.CreateCommand();
        }

        public void Open()
        {
            connection.Open();
        }

        public Task OpenAsync()
        {
            return connection.OpenAsync();
        }

        public Task OpenAsync(CancellationToken token)
        {
            return connection.OpenAsync(token);
        } 

        #endregion

        #endregion

        #region IDisposable Implementation

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                    connection.Dispose();
                }
                disposed = true;
            }
        }

        ~SqlClientConnection()
        {
            Dispose(false);
        }

        #endregion

    }
}
