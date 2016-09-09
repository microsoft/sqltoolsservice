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
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// Provides a reliable way of opening connections to and executing commands
    /// taking into account potential network unreliability and a requirement for connection retry.
    /// </summary>
    internal sealed partial class ReliableSqlConnection
    {
        internal class ReliableSqlCommand : IDbCommand
        {
            private const int Dummy = 0;
            private readonly SqlCommand _command;

            // connection is settable
            private ReliableSqlConnection _connection;

            public ReliableSqlCommand()
                : this(null, Dummy)
            {
            }

            public ReliableSqlCommand(ReliableSqlConnection connection)
                : this(connection, Dummy)
            {
                Contract.Requires(connection != null);
            }

            private ReliableSqlCommand(ReliableSqlConnection connection, int dummy)
            {
                if (connection != null)
                {
                    _connection = connection;
                    _command = connection.CreateSqlCommand();
                }
                else
                {
                    _command = new SqlCommand();
                }
            }

            public void Dispose()
            {
                _command.Dispose();
            }

            /// <summary>
            /// Gets or sets the text command to run against the data source.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
            public string CommandText
            {
                get { return _command.CommandText; }
                set { _command.CommandText = value; }
            }

            /// <summary>
            /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
            /// </summary>
            public int CommandTimeout
            {
                get { return _command.CommandTimeout; }
                set { _command.CommandTimeout = value; }
            }

            /// <summary>
            /// Gets or sets a value that specifies how the <see cref="System.Data.Common.DbCommand.CommandText"/> property is interpreted.
            /// </summary>
            public CommandType CommandType
            {
                get { return _command.CommandType; }
                set { _command.CommandType = value; }
            }

            /// <summary>
            /// Gets or sets the <see cref="System.Data.Common.DbConnection"/> used by this <see cref="System.Data.Common.DbCommand"/>.
            /// </summary>
            public IDbConnection Connection
            {
                get
                {
                    return _connection;
                }

                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("value");
                    }

                    ReliableSqlConnection newConnection = value as ReliableSqlConnection;

                    if (newConnection == null)
                    {
                        throw new InvalidOperationException(Resources.OnlyReliableConnectionSupported);
                    }

                    _connection = newConnection;
                    _command.Connection = _connection._underlyingConnection;
                }
            }

            /// <summary>
            /// Gets the <see cref="System.Data.IDataParameterCollection"/>.
            /// </summary>
            public IDataParameterCollection Parameters
            {
                get { return _command.Parameters; }
            }

            /// <summary>
            /// Gets or sets the transaction within which the Command object of a .NET Framework data provider executes.
            /// </summary>
            public IDbTransaction Transaction
            {
                get { return _command.Transaction; }
                set { _command.Transaction = value as SqlTransaction; }
            }

            /// <summary>
            /// Gets or sets how command results are applied to the System.Data.DataRow when
            /// used by the System.Data.IDataAdapter.Update(System.Data.DataSet) method of
            /// a <see cref="System.Data.Common.DbDataAdapter"/>.
            /// </summary>
            public UpdateRowSource UpdatedRowSource
            {
                get { return _command.UpdatedRowSource; }
                set { _command.UpdatedRowSource = value; }
            }

            /// <summary>
            /// Attempts to cancels the execution of an <see cref="System.Data.IDbCommand"/>.
            /// </summary>
            public void Cancel()
            {
                _command.Cancel();
            }

            /// <summary>
            /// Creates a new instance of an <see cref="System.Data.IDbDataParameter"/> object.
            /// </summary>
            /// <returns>An <see cref="IDbDataParameter"/> object.</returns>
            public IDbDataParameter CreateParameter()
            {
                return _command.CreateParameter();
            }

            /// <summary>
            /// Executes an SQL statement against the Connection object of a .NET Framework
            /// data provider, and returns the number of rows affected.
            /// </summary>
            /// <returns>The number of rows affected.</returns>
            public int ExecuteNonQuery()
            {
                ValidateConnectionIsSet();
                return _connection.ExecuteNonQuery(_command);
            }

            /// <summary>
            /// Executes the <see cref="System.Data.IDbCommand.CommandText"/> against the <see cref="System.Data.IDbCommand.Connection"/>
            /// and builds an <see cref="System.Data.IDataReader"/>.
            /// </summary>
            /// <returns>An <see cref="System.Data.IDataReader"/> object.</returns>
            public IDataReader ExecuteReader()
            {
                ValidateConnectionIsSet();
                return _connection.ExecuteReader(_command, CommandBehavior.Default);
            }

            /// <summary>
            /// Executes the <see cref="System.Data.IDbCommand.CommandText"/> against the <see cref="System.Data.IDbCommand.Connection"/>
            /// and builds an <see cref="System.Data.IDataReader"/> using one of the <see cref="System.Data.CommandBehavior"/> values.
            /// </summary>
            /// <param name="behavior">One of the <see cref="System.Data.CommandBehavior"/> values.</param>
            /// <returns>An <see cref="System.Data.IDataReader"/> object.</returns>
            public IDataReader ExecuteReader(CommandBehavior behavior)
            {
                ValidateConnectionIsSet();
                return _connection.ExecuteReader(_command, behavior);
            }

            /// <summary>
            /// Executes the query, and returns the first column of the first row in the
            /// resultset returned by the query. Extra columns or rows are ignored.
            /// </summary>
            /// <returns>The first column of the first row in the resultset.</returns>
            public object ExecuteScalar()
            {
                ValidateConnectionIsSet();
                return _connection.ExecuteScalar(_command);
            }

            /// <summary>
            /// Creates a prepared (or compiled) version of the command on the data source.
            /// </summary>
            public void Prepare()
            {
                _command.Prepare();
            }

            internal SqlCommand GetUnderlyingCommand()
            {
                return _command;
            }

            private void ValidateConnectionIsSet()
            {
                if (_connection == null)
                {
                    throw new InvalidOperationException(Resources.ConnectionPropertyNotSet);
                }
            }
        }
    }
}
