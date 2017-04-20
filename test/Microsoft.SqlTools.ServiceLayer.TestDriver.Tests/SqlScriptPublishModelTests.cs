//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Scripting service end-to-end integration tests that use the SqlScriptPublishModel type to generate scripts.
    /// </summary>
    public class SqlScriptPublishModelTests : IClassFixture<SqlScriptPublishModelTests.ScriptingFixture>
    {
        public SqlScriptPublishModelTests(ScriptingFixture scriptingFixture)
        {
            this.Fixture = scriptingFixture;
        }

        public ScriptingFixture Fixture { get; private set; }

        public SqlTestDb Northwind { get { return this.Fixture.Database; } }

        [Fact]
        public async Task ListSchemaObjects()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ScriptingListObjectsParams requestParams = new ScriptingListObjectsParams
                {
                    ConnectionString = this.Northwind.ConnectionString,
                };

                ScriptingListObjectsResult result = await testService.ListScriptingObjects(requestParams);
                ScriptingListObjectsCompleteParams completeParameters = await testService.Driver.WaitForEvent(ScriptingListObjectsCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.Equal<int>(ScriptingFixture.ObjectCountWithoutDatabase, completeParameters.DatabaseObjects.Count);
            }
        }

        [Fact]
        public async Task ScriptDatabaseSchema()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = this.Northwind.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaOnly",
                    },
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingPlanNotificationParams planEvent = await testService.Driver.WaitForEvent(ScriptingPlanNotificationEvent.Type, TimeSpan.FromSeconds(30));
                ScriptingCompleteParams parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.True(parameters.Success);
                Assert.Equal<int>(ScriptingFixture.ObjectCountWithDatabase, planEvent.Count);
                Assert.True(File.Exists(tempFile.FilePath));
                Assert.True(new FileInfo(tempFile.FilePath).Length > 0);
            }
        }

        [Fact]
        public async Task ScriptDatabaseSchemaAndData()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = this.Northwind.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaAndData",
                    },
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingPlanNotificationParams planEvent = await testService.Driver.WaitForEvent(ScriptingPlanNotificationEvent.Type, TimeSpan.FromSeconds(30));
                ScriptingCompleteParams completeParameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.True(completeParameters.Success);
                Assert.Equal<int>(ScriptingFixture.ObjectCountWithDatabase, planEvent.Count);
                Assert.True(File.Exists(tempFile.FilePath));
                Assert.True(new FileInfo(tempFile.FilePath).Length > 0);
            }
        }

        [Fact]
        public async Task ScriptTable()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ScriptingParams requestParams = new ScriptingParams
                {                    
                    FilePath = tempFile.FilePath,
                    ConnectionString = this.Northwind.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaOnly",
                    },
                    ScriptingObjects = new List<ScriptingObject>
                    {
                        new ScriptingObject
                        {
                            Type = "Table",
                            Schema = "dbo",
                            Name = "Customers",
                        },
                    }
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingPlanNotificationParams planEvent = await testService.Driver.WaitForEvent(ScriptingPlanNotificationEvent.Type, TimeSpan.FromSeconds(30));
                ScriptingCompleteParams parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.True(parameters.Success);
                Assert.Equal<int>(2, planEvent.Count);
                Assert.True(File.Exists(tempFile.FilePath));
                Assert.True(new FileInfo(tempFile.FilePath).Length > 0);
            }
        }

        [Fact]
        public async Task ScriptTableUsingIncludeFilter()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = this.Northwind.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaOnly",
                    },
                    IncludeObjectCriteria = new List<ScriptingObject>
                    {
                        new ScriptingObject
                        {
                            Type = "Table",
                            Schema = "dbo",
                            Name = "Customers",
                        },
                    }
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingPlanNotificationParams planEvent = await testService.Driver.WaitForEvent(ScriptingPlanNotificationEvent.Type, TimeSpan.FromSeconds(30));
                ScriptingCompleteParams parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.True(parameters.Success);
                Assert.Equal<int>(2, planEvent.Count);
                Assert.True(File.Exists(tempFile.FilePath));
                Assert.True(new FileInfo(tempFile.FilePath).Length > 0);
            }
        }

        [Fact]
        public async Task ScriptTableAndData()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = this.Northwind.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaAndData",
                    },
                    ScriptingObjects = new List<ScriptingObject>
                    {
                        new ScriptingObject
                        {
                            Type = "Table",
                            Schema = "dbo",
                            Name = "Customers",
                        },
                    }
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingPlanNotificationParams planEvent = await testService.Driver.WaitForEvent(ScriptingPlanNotificationEvent.Type, TimeSpan.FromSeconds(30));
                ScriptingCompleteParams parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.True(parameters.Success);
                Assert.Equal<int>(2, planEvent.Count);
                Assert.True(File.Exists(tempFile.FilePath));
                Assert.True(new FileInfo(tempFile.FilePath).Length > 0);
            }
        }

        [Fact]
        public async Task ScriptTableDoesNotExist()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = this.Northwind.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaOnly",
                    },
                    ScriptingObjects = new List<ScriptingObject>
                    {
                        new ScriptingObject
                        {
                            Type = "Table",
                            Schema = "dbo",
                            Name = "TableDoesNotExist",
                        },
                    }
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingCompleteParams parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.True(parameters.HasError);
                Assert.Equal("An error occurred while scripting the objects.", parameters.ErrorMessage);
                Assert.Contains("The Table '[dbo].[TableDoesNotExist]' does not exist on the server.", parameters.ErrorDetails);
            }
        }

        [Fact]
        public async Task ScriptSchemaCancel()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = this.Northwind.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaAndData",
                    },
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingCancelResult cancelResult = await testService.CancelScript(result.OperationId);
                ScriptingCompleteParams cancelEvent = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.True(cancelEvent.Canceled);
            }
        }


        [Fact]
        public async Task ScriptSchemaInvalidConnectionString()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            {
                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = "I'm an invalid connection string",
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaAndData",
                    },
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingCompleteParams parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.True(parameters.HasError);
                Assert.Equal("Error parsing ScriptingParams.ConnectionString property", parameters.ErrorMessage);
            }
        }

        [Fact]
        public async Task ScriptSchemaInvalidFilePath()
        {
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = "This path doesn't event exist",
                    ConnectionString = "Server=Temp;Database=Temp;User Id=Temp;Password=Temp",
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaAndData",
                    },
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingCompleteParams parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromSeconds(30));
                Assert.True(parameters.HasError);
                Assert.Equal("Invalid directory specified by the ScriptingParams.FilePath property.", parameters.ErrorMessage);
            }
        }

        public void Dispose() { }

        public class ScriptingFixture : IDisposable
        {
            public ScriptingFixture()
            {
                this.Database = SqlTestDb.CreateNew(TestServerType.OnPrem);
                this.Database.RunQuery(Scripts.CreateNorthwindSchema, throwOnError: true);
                Console.WriteLine("Northwind setup complete, database name: {0}", this.Database.DatabaseName);
            }

            /// <summary>
            /// The count of object when scripting the entire database including the database object.
            /// </summary>
            public const int ObjectCountWithDatabase = 46;

            /// <summary>
            /// The count of objects when scripting the entire database excluding the database object.
            /// </summary>
            public const int ObjectCountWithoutDatabase = 45;

            public SqlTestDb Database { get; private set; }

            public void Dispose()
            {
                if (this.Database != null)
                {
                    Console.WriteLine("Northwind cleanup, deleting database name: {0}", this.Database.DatabaseName);
                    this.Database.Dispose();
                }
            }
        }
   }
}
