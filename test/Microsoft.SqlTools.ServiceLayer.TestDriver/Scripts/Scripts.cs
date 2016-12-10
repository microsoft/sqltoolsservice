//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts
{
    public class Scripts
    {
        public const string MasterBasicQuery = "SELECT * FROM sys.all_columns"; //basic queries should return at least 10000 rows

        public const string DelayQuery = "WAITFOR DELAY '00:01:00'";

        public const string TestDbSimpleSelectQuery = "SELECT * FROM [Person].[Address]";

        public const string SelectQuery = "SELECT * FROM ";

        public static string CreateDatabaseObjectsQuery { get { return CreateDatabaseObjectsQueryInstance.Value; } }

        public static string CreateDatabaseQuery { get { return CreateDatabaseQueryInstance.Value; } }

        public static string TestDbComplexSelectQueries { get { return TestDbSelectQueriesInstance.Value; } }

        private static readonly Lazy<string> CreateDatabaseObjectsQueryInstance = new Lazy<string>(() =>
        {
            return GetScriptFileContent("Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts.CreateTestDatabaseObjects.sql");
        });

        private static readonly Lazy<string> CreateDatabaseQueryInstance = new Lazy<string>(() =>
        {
            return GetScriptFileContent("Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts.CreateTestDatabase.sql");
        });

        private static readonly Lazy<string> TestDbSelectQueriesInstance = new Lazy<string>(() =>
        {
            return GetScriptFileContent("Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts.TestDbTableQueries.sql");
        });

        private static string GetScriptFileContent(string fileName)
        {
            string fileContent = string.Empty;
            try
            {
                using (Stream stream = typeof(Scripts).GetTypeInfo().Assembly.GetManifestResourceStream(fileName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load the sql script. error: {ex.Message}");
            }
            return fileContent;
        }

       
    }
}
