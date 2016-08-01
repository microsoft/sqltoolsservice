//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//#define USE_LIVE_CONNECTION

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.ConnectionServices;
using Microsoft.SqlTools.ServiceLayer.ConnectionServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Xunit;

namespace Microsoft.SqlTools.Test.Utility
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests
    /// </summary>
    public class TestObjects
    {
        /// <summary>
        /// Creates a test connection service
        /// </summary>
        public static ConnectionService GetTestConnectionService()
        {
#if !USE_LIVE_CONNECTION
            // use mock database connection
            return new ConnectionService(new TestSqlConnectionFactory());
#else
            // connect to a real server instance
            return ConnectionService.Instance;
#endif
        }

        /// <summary>
        /// Creates a test connection details object
        /// </summary>
        public static ConnectionDetails GetTestConnectionDetails()
        {
            return new ConnectionDetails()
            {
                UserName = "sa",
                Password = "Yukon900",
                DatabaseName = "AdventureWorks2016CTP3_2",
                ServerName = "sqltools11"
            };
        }

        /// <summary>
        /// Create a test language service instance
        /// </summary>
        /// <returns></returns>
        public static LanguageService GetTestLanguageService()
        {
            return new LanguageService();
        }

        /// <summary>
        /// Creates a test autocomplete service instance
        /// </summary>
        public static AutoCompleteService GetAutoCompleteService()
        {
            return AutoCompleteService.Instance;
        }

        /// <summary>
        /// Creates a test sql connection factory instance
        /// </summary>
        public static ISqlConnectionFactory GetTestSqlConnectionFactory()
        {
#if !USE_LIVE_CONNECTION
            // use mock database connection
            return new TestSqlConnectionFactory();
#else
            // connect to a real server instance
            return ConnectionService.Instance.ConnectionFactory;
#endif
            
        }
    }

    public class TestSqlReader : IDataReader
    {
        
        #region Test Specific Implementations

        internal string SqlCommandText { get; set; }

        private const string tableNameTestCommand = "SELECT name FROM sys.tables";

        private List<Dictionary<string, string>> tableNamesTest = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string> { {"name", "table1"} },
            new Dictionary<string, string> { {"name", "table2"} }
        };

        private IEnumerator<Dictionary<string, string>> tableEnumerator;

        #endregion

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            throw new NotImplementedException();
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public string GetName(int i)
        {
            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            throw new NotImplementedException();
        }

        public object GetValue(int i)
        {
            throw new NotImplementedException();
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            throw new NotImplementedException();
        }

        public int FieldCount { get; }

        object IDataRecord.this[string name]
        {
            get { return tableEnumerator.Current[name]; }
        }

        object IDataRecord.this[int i]
        {
            get { return tableEnumerator.Current[tableEnumerator.Current.Keys.ToArray()[i]]; }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        public bool Read()
        {
            if (tableEnumerator == null)
            {
                switch (SqlCommandText)
                {
                    case tableNameTestCommand:
                        tableEnumerator = ((IEnumerable<Dictionary<string, string>>)tableNamesTest).GetEnumerator();
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            return tableEnumerator.MoveNext();
        }

        public int Depth { get; }
        public bool IsClosed { get; }
        public int RecordsAffected { get; }
    }

    /// <summary>
    /// Test mock class for IDbCommand
    /// </summary>
    public class TestSqlCommand : IDbCommand
    {

        public string CommandText { get; set; }

        public int CommandTimeout { get; set; }

        public CommandType CommandType { get; set; }

        public IDbConnection Connection { get; set; }

        public IDataParameterCollection Parameters
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IDbTransaction Transaction { get; set; }

        public UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public IDbDataParameter CreateParameter()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public IDataReader ExecuteReader()
        {
            return new TestSqlReader
            {
                SqlCommandText = CommandText
            };
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            throw new NotImplementedException();
        }

        public object ExecuteScalar()
        {
            throw new NotImplementedException();
        }

        public void Prepare()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Test mock class for SqlConnection wrapper
    /// </summary>
    public class TestSqlConnection : ISqlConnection
    {
        public TestSqlConnection(string connectionString)
        {
            
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public IDbTransaction BeginTransaction()
        {
            throw new System.NotImplementedException();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            throw new System.NotImplementedException();
        }

        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public IDbCommand CreateCommand()
        {
            return new TestSqlCommand {Connection = this};
        }

        public void Open()
        {
            // No Op.
        }

        public string ConnectionString { get; set; }
        public int ConnectionTimeout { get; }
        public string Database { get; }
        public ConnectionState State { get; }

        public void ChangeDatabase(string databaseName)
        {
            throw new System.NotImplementedException();
        }

        public string DataSource { get; }
        public string ServerVersion { get; }
        public void ClearPool()
        {
            throw new System.NotImplementedException();
        }

        public async Task OpenAsync()
        {
            // No Op.
            await Task.FromResult(0);
        }

        public Task OpenAsync(CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }

    /// <summary>
    /// Test mock class for SqlConnection factory
    /// </summary>
    public class TestSqlConnectionFactory : ISqlConnectionFactory
    {
        public ISqlConnection CreateSqlConnection(string connectionString)
        {
            return new TestSqlConnection(connectionString);
        }
    }
}
