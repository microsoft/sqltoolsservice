//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Scripting service end-to-end integration tests.
    /// </summary>
    public class ScriptingTests : IDisposable
    {
        /// <summary>
        /// The count of object when scripting the entire database, which includes the database as an object.
        /// </summary>
        public const int NorthwindObjectCount = 46;

        /// <summary>
        /// The count of schema object, which excludes the database object.
        /// </summary>
        public const int NorthwindSchemaObjectCount = 45;

        public void Dispose() {}

        [Fact]
        public async Task ListSchemaObjects()
        {
            using (SqlTestDb testDatabase = SqlTestDb.CreateNew(TestServerType.OnPrem))
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                testDatabase.RunQuery(Scripts.CreateNorthwindSchema, throwOnError: true);

                ScriptingListObjectsParams requestParams = new ScriptingListObjectsParams
                {
                    ConnectionString = testDatabase.ConnectionString,
                };

                ScriptingListObjectsResult result = await testService.ListScriptingObjects(requestParams);
                ScriptingListObjectsCompleteParameters completeParameters = await testService.Driver.WaitForEvent(ScriptingListObjectsCompleteEvent.Type, TimeSpan.FromMinutes(1));
                Assert.Equal<int>(NorthwindSchemaObjectCount, completeParameters.DatabaseObjects.Count);
                testService.AssertEventNotQueued(ScriptingErrorEvent.Type);
            }
        }

        [Fact]
        public async Task ScriptDatabaseSchema()
        {
            using (SqlTestDb testDatabase = SqlTestDb.CreateNew(TestServerType.OnPrem))
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                testDatabase.RunQuery(Scripts.CreateNorthwindSchema, throwOnError: true);

                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = testDatabase.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaOnly",
                    },
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingPlanNotificationParams planEvent = await testService.Driver.WaitForEvent(ScriptingPlanNotificationEvent.Type, TimeSpan.FromMinutes(1));
                ScriptingCompleteParameters parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromMinutes(1));
                Assert.Equal<int>(NorthwindObjectCount, planEvent.Count);
                testService.AssertEventNotQueued(ScriptingErrorEvent.Type);
            }
        }

        [Fact]
        public async Task ScriptDatabaseSchemaAndData()
        {
            using (SqlTestDb testDatabase = SqlTestDb.CreateNew(TestServerType.OnPrem))
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                testDatabase.RunQuery(Scripts.CreateNorthwindSchema, throwOnError: true);

                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = testDatabase.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaAndData",
                    },
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingPlanNotificationParams planEvent = await testService.Driver.WaitForEvent(ScriptingPlanNotificationEvent.Type, TimeSpan.FromMinutes(1));
                ScriptingCompleteParameters completeParameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromMinutes(1));
                Assert.Equal<int>(NorthwindObjectCount, planEvent.Count);
                testService.AssertEventNotQueued(ScriptingErrorEvent.Type);
            }
        }

        [Fact]
        public async Task ScriptTable()
        {
            using (SqlTestDb testDatabase = SqlTestDb.CreateNew(TestServerType.OnPrem))
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                testDatabase.RunQuery(Scripts.CreateNorthwindSchema, throwOnError: true);

                ScriptingParams requestParams = new ScriptingParams
                {                    
                    FilePath = tempFile.FilePath,
                    ConnectionString = testDatabase.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaOnly",
                    },
                    DatabaseObjects = new List<ScriptingObject>
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
                ScriptingPlanNotificationParams planEvent = await testService.Driver.WaitForEvent(ScriptingPlanNotificationEvent.Type, TimeSpan.FromMinutes(1));
                ScriptingCompleteParameters parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromMinutes(1));
                Assert.Equal<int>(1, planEvent.Count);
                testService.AssertEventNotQueued(ScriptingErrorEvent.Type);
            }
        }

        [Fact]
        public async Task ScriptTableAndData()
        {
            using (SqlTestDb testDatabase = SqlTestDb.CreateNew(TestServerType.OnPrem))
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                testDatabase.RunQuery(Scripts.CreateNorthwindSchema, throwOnError: true);

                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = testDatabase.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaAndData",
                    },
                    DatabaseObjects = new List<ScriptingObject>
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
                ScriptingPlanNotificationParams planEvent = await testService.Driver.WaitForEvent(ScriptingPlanNotificationEvent.Type, TimeSpan.FromMinutes(1));
                ScriptingCompleteParameters parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromMinutes(1));
                Assert.Equal<int>(1, planEvent.Count);
                testService.AssertEventNotQueued(ScriptingErrorEvent.Type);
            }
        }

        [Fact]
        public async Task ScriptTableDoesNotExist()
        {
            using (SqlTestDb testDatabase = SqlTestDb.CreateNew(TestServerType.OnPrem))
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                testDatabase.RunQuery(Scripts.CreateNorthwindSchema, throwOnError: true);

                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = testDatabase.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaOnly",
                    },
                    DatabaseObjects = new List<ScriptingObject>
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
                ScriptingErrorParams parameters = await testService.Driver.WaitForEvent(ScriptingErrorEvent.Type, TimeSpan.FromMinutes(1));
                Assert.Equal("An error occurred while scripting the objects.", parameters.Message);
                Assert.Contains("The Table '[dbo].[TableDoesNotExist]' does not exist on the server.", parameters.DiagnosticMessage);
            }
        }

        [Fact]
        public async Task ScriptSchemaCancel()
        {
            using (SqlTestDb testDatabase = SqlTestDb.CreateNew(TestServerType.OnPrem))
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                testDatabase.RunQuery(Scripts.CreateNorthwindSchema, throwOnError: true);

                ScriptingParams requestParams = new ScriptingParams
                {
                    FilePath = tempFile.FilePath,
                    ConnectionString = testDatabase.ConnectionString,
                    ScriptOptions = new ScriptOptions
                    {
                        TypeOfDataToScript = "SchemaAndData",
                    },
                };

                ScriptingResult result = await testService.Script(requestParams);
                ScriptingCancelResult cancelResult = await testService.CancelScript(result.OperationId);
                ScriptingCancelParameters cancelEvent = await testService.Driver.WaitForEvent(ScriptingCancelEvent.Type, TimeSpan.FromMinutes(1));
                testService.AssertEventNotQueued(ScriptingErrorEvent.Type);
            }
        }


        [Fact]
        public async Task ScriptSchemaInvalidConnectionString()
        {
            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
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
                ScriptingErrorParams errorEvent = await testService.Driver.WaitForEvent(ScriptingErrorEvent.Type, TimeSpan.FromMinutes(1));
                Assert.Equal("Error parsing ConnectionString property", errorEvent.Message);
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
                ScriptingErrorParams errorEvent = await testService.Driver.WaitForEvent(ScriptingErrorEvent.Type, TimeSpan.FromMinutes(1));
                Assert.Equal("Invalid directory specified by the FilePath property.", errorEvent.Message);
            }
        }
    }
}
