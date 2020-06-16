//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// This code is copied from the source described in the comment below.

// =======================================================================================
// Microsoft Windows Server AppFabric Customer Advisory Team (CAT) Best Practices Series
//
// This sample is supplemental to the technical guidance published on the community
// blog at http://blogs.msdn.com/appfabriccat/ and  copied from
// sqlmain ./sql/manageability/mfx/common/
//
// =======================================================================================
// Copyright © 2012 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
// =======================================================================================

// namespace Microsoft.AppFabricCAT.Samples.Azure.TransientFaultHandling.SqlAzure
// namespace Microsoft.SqlServer.Management.Common

using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.Kusto.ServiceLayer.DataSource;

namespace Microsoft.Kusto.ServiceLayer.Connection
{
    /// <summary>
    /// Provides a reliable way of opening connections to and executing commands
    /// taking into account potential network unreliability and a requirement for connection retry.
    /// </summary>
    public partial class ReliableDataSourceConnection : IDisposable
    {
        private IDataSource _dataSource;
        private readonly RetryPolicy _connectionRetryPolicy;
        private RetryPolicy _commandRetryPolicy;
        private Guid _azureSessionId = Guid.NewGuid();

        private string _connectionString;
        private string _azureAccountToken;

        /// <summary>
        /// Initializes a new instance of the ReliableKustoClient class with a given connection string
        /// and a policy defining whether to retry a request if the connection fails to be opened or a command
        /// fails to be successfully executed.
        /// </summary>
        /// <param name="connectionString">The connection string used to open the SQL Azure database.</param>
        /// <param name="connectionRetryPolicy">The retry policy defining whether to retry a request if a connection fails to be established.</param>
        /// <param name="commandRetryPolicy">The retry policy defining whether to retry a request if a command fails to be executed.</param>
        public ReliableDataSourceConnection(string connectionString, RetryPolicy connectionRetryPolicy, RetryPolicy commandRetryPolicy, string azureAccountToken)
        {
            _connectionString = connectionString;
            _azureAccountToken = azureAccountToken;
            _dataSource = DataSourceFactory.Create(DataSourceType.Kusto, connectionString, azureAccountToken);
            
            _connectionRetryPolicy = connectionRetryPolicy ?? RetryPolicyFactory.CreateNoRetryPolicy();
            _commandRetryPolicy = commandRetryPolicy ?? RetryPolicyFactory.CreateNoRetryPolicy();

            _connectionRetryPolicy.RetryOccurred += RetryConnectionCallback;
            _commandRetryPolicy.RetryOccurred += RetryCommandCallback;
        }

        /// <summary>
        /// Used for Unit Testing
        /// </summary>
        /// <param name="dataSource"></param>
        protected ReliableDataSourceConnection(IDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        /// <summary>
        /// Used for Unit Testing
        /// </summary>
        public ReliableDataSourceConnection()
        {
            
        }

        private void RetryCommandCallback(RetryState retryState)
        {
            RetryPolicyUtils.RaiseSchemaAmbientRetryMessage(retryState, SqlSchemaModelErrorCodes.ServiceActions.CommandRetry, _azureSessionId); 
        }

        private void RetryConnectionCallback(RetryState retryState)
        {
            RetryPolicyUtils.RaiseSchemaAmbientRetryMessage(retryState, SqlSchemaModelErrorCodes.ServiceActions.ConnectionRetry, _azureSessionId); 
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        ///  resetting managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">A flag indicating that managed resources must be released.</param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_connectionRetryPolicy != null)
                {
                    _connectionRetryPolicy.RetryOccurred -= RetryConnectionCallback;
                }

                if (_commandRetryPolicy != null)
                {
                    _commandRetryPolicy.RetryOccurred -= RetryCommandCallback;
                }

                _dataSource.Dispose();
            }
        }

        /// <summary>
        /// Gets or sets the connection string for opening a connection to the SQL Azure database.
        /// </summary>
        public string ConnectionString { get; set; }
        
        /// <summary>
        /// Gets the policy which decides whether to retry a connection request, based on how many
        /// times the request has been made and the reason for the last failure. 
        /// </summary>
        public RetryPolicy ConnectionRetryPolicy
        {
            get { return _connectionRetryPolicy; }
        }

        /// <summary>
        /// Gets the policy which decides whether to retry a command, based on how many
        /// times the request has been made and the reason for the last failure. 
        /// </summary>
        public RetryPolicy CommandRetryPolicy
        {
            get { return _commandRetryPolicy; }
            set
            {
                Validate.IsNotNull(nameof(value), value);

                if (_commandRetryPolicy != null)
                {
                    _commandRetryPolicy.RetryOccurred -= RetryCommandCallback;
                }

                _commandRetryPolicy = value;
                _commandRetryPolicy.RetryOccurred += RetryCommandCallback;
            }
        }

        /// <summary>
        /// Gets the server name from the underlying connection.
        /// </summary>
        public string ClusterName
        {
            get { return _dataSource.ClusterName; }
        }

        /// <summary>
        /// If the underlying SqlConnection absolutely has to be accessed, for instance
        /// to pass to external APIs that require this type of connection, then this
        /// can be used.  
        /// </summary>
        /// <returns><see cref="SqlConnection"/></returns>
        public virtual IDataSource GetUnderlyingConnection()
        {
            return _dataSource;
        }

        /// <summary>
        /// Changes the current database for an open Connection object.
        /// </summary>
        /// <param name="databaseName">The name of the database to use in place of the current database.</param>
        public virtual void ChangeDatabase(string databaseName)
        {
            _dataSource.DatabaseName = databaseName;
        }

        /// <summary>
        /// Opens a database connection with the settings specified by the ConnectionString
        /// property of the provider-specific Connection object.
        /// </summary>
        public virtual void Open()
        {
            // TODOKusto: Should we initialize in the constructor or here. Set a breapoint and check.
            // Check if retry policy was specified, if not, disable retries by executing the Open method using RetryPolicy.NoRetry.
            if(_dataSource == null)
            {
                _connectionRetryPolicy.ExecuteAction(() =>
                {
                    _dataSource = DataSourceFactory.Create(DataSourceType.Kusto, _connectionString, _azureAccountToken);
                });
            }
        }

        /// <summary>
        /// Opens a database connection with the settings specified by the ConnectionString
        /// property of the provider-specific Connection object.
        /// </summary>
        public virtual Task OpenAsync(CancellationToken token)
        {
            // Make sure that the token isn't cancelled before we try
            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled(token);
            }

            // Check if retry policy was specified, if not, disable retries by executing the Open method using RetryPolicy.NoRetry.
            try
            {
                return _connectionRetryPolicy.ExecuteAction(async () =>
                {
                    await Task.Run(() => Open());
                });
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        /// <summary>
        /// Closes the connection to the database.
        /// </summary>
        public virtual void Close()
        {
        }

        /// <summary>
        /// Gets the time to wait while trying to establish a connection before terminating
        /// the attempt and generating an error.
        /// </summary>
        public int ConnectionTimeout
        {
            get { return 30; }
        }

        /// <summary>
        /// Gets the name of the current database or the database to be used after a
        /// connection is opened.
        /// </summary>
        public virtual string Database
        {
            get { return _dataSource.DatabaseName; }
        }

        private void VerifyConnectionOpen(ReliableDataSourceConnection conn)
        {
            if(conn.GetUnderlyingConnection() == null)
            {
                conn.Open();
            }
        }
    }
}

