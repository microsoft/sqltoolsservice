//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//#define USE_LIVE_CONNECTION

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
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
            return new LanguageService(new SqlToolsContext(null, null));
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

    /// <summary>
    /// Test mock class for SqlConnection wrapper
    /// </summary>
    public class TestSqlConnection : ISqlConnection
    {
        public void OpenDatabaseConnection(string connectionString)
        {
        }

        public IEnumerable<string> GetServerObjects()
        {
            return null;
        }
    }

    /// <summary>
    /// Test mock class for SqlConnection factory
    /// </summary>
    public class TestSqlConnectionFactory : ISqlConnectionFactory
    {
        public ISqlConnection CreateSqlConnection()
        {
            return new TestSqlConnection();
        }
    }
}
