//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//#define USE_LIVE_CONNECTION

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
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

    public class TestDataReader : DbDataReader
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

        public override bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override decimal GetDecimal(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override double GetDouble(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public override string GetName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetInt64(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetInt32(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetString(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override bool NextResult()
        {
            throw new NotImplementedException();
        }

        public override bool Read()
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

        public override int Depth { get; }
        public override bool IsClosed { get; }
        public override int RecordsAffected { get; }

        public override object this[string name]
        {
            get { return tableEnumerator.Current[name]; }
        }

        public override object this[int ordinal]
        {
            get { return tableEnumerator.Current[tableEnumerator.Current.Keys.ToArray()[ordinal]]; }
        }

        public override int FieldCount { get; }
        public override bool HasRows { get; }
    }

    /// <summary>
    /// Test mock class for IDbCommand
    /// </summary>
    public class TestSqlCommand : DbCommand
    {
        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        public override int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public override object ExecuteScalar()
        {
            throw new NotImplementedException();
        }

        public override void Prepare()
        {
            throw new NotImplementedException();
        }

        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; }
        protected override DbTransaction DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return new TestDataReader {SqlCommandText = CommandText};
        }
    }

    /// <summary>
    /// Test mock class for SqlConnection wrapper
    /// </summary>
    public class TestSqlConnection : DbConnection
    {
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            throw new NotImplementedException();
        }

        public override void Open()
        {
            // No Op
        }

        public override string ConnectionString { get; set; }
        public override string Database { get; }
        public override ConnectionState State { get; }
        public override string DataSource { get; }
        public override string ServerVersion { get; }

        protected override DbCommand CreateDbCommand()
        {
            return new TestSqlCommand();
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Test mock class for SqlConnection factory
    /// </summary>
    public class TestSqlConnectionFactory : ISqlConnectionFactory
    {
        public DbConnection CreateSqlConnection(string connectionString)
        {
            return new TestSqlConnection()
            {
                ConnectionString = connectionString
            };
        }
    }
}
