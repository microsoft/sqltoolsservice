//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class Scripts
    {
        private const string ResourceNameRefix = "Microsoft.SqlTools.ServiceLayer.Test.Common.Scripts.";

        public const string MasterBasicQuery = "SELECT * FROM sys.all_columns"; //basic queries should return at least 10000 rows

        public const string DelayQuery = "WAITFOR DELAY '00:01:00'";

        public const string TestDbSimpleSelectQuery = "SELECT * FROM [Person].[Address]";

        public const string SelectQuery = "SELECT * FROM ";

        public const string DropDatabaseIfExist = @"
IF EXISTS (SELECT 1 FROM [sys].[databases] WHERE [name] = '{0}') 
BEGIN
    ALTER DATABASE [{0}]
    SET READ_WRITE;
    ALTER DATABASE [{0}]
    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{0}];
END
";

        public const string DropDatabaseIfExistAzure = @"
IF EXISTS (SELECT 1 FROM [sys].[databases] WHERE [name] = '{0}') 
BEGIN
    DROP DATABASE [{0}];
END
";

        public static string CreateDatabaseObjectsQuery { get { return CreateDatabaseObjectsQueryInstance.Value; } }

        public static string CreateDatabaseQuery { get { return CreateDatabaseQueryInstance.Value; } }

        public static string TestDbComplexSelectQueries { get { return TestDbSelectQueriesInstance.Value; } }

        public static string AdventureWorksScript { get { return AdventureWorksScriptInstance.Value; } }

        public static string CreateNorthwindSchema { get { return CreateNorthwindSchemaInstance.Value; } }

        private static readonly Lazy<string> CreateDatabaseObjectsQueryInstance = new Lazy<string>(() =>
        {
            return GetScriptFileContent(ResourceNameRefix + "CreateTestDatabaseObjects.sql");
        });

        private static readonly Lazy<string> CreateDatabaseQueryInstance = new Lazy<string>(() =>
        {
            return GetScriptFileContent(ResourceNameRefix + "CreateTestDatabase.sql");
        });

        private static readonly Lazy<string> TestDbSelectQueriesInstance = new Lazy<string>(() =>
        {
            return GetScriptFileContent(ResourceNameRefix + "TestDbTableQueries.sql");
        });

        private static readonly Lazy<string> AdventureWorksScriptInstance = new Lazy<string>(() =>
        {
            return GetScriptFileContent(ResourceNameRefix + "AdventureWorks.sql");
        });

        private static readonly Lazy<string> CreateNorthwindSchemaInstance = new Lazy<string>(() =>
        {
            return GetScriptFileContent(ResourceNameRefix + "CreateNorthwindSchema.sql");
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
