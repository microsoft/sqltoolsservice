//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts;
using System.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Scripting service end-to-end integration tests.
    /// </summary>
    public class ScriptingTests : IDisposable
    {
        public const int NorthwindObjectCount = 46;

        public void Dispose() {}

        [Fact]
        public async Task ScriptSchema()
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
        public async Task ScriptSchemaAndData()
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
                ScriptingCompleteParameters parameters = await testService.Driver.WaitForEvent(ScriptingCompleteEvent.Type, TimeSpan.FromMinutes(1));

                Assert.Equal<int>(NorthwindObjectCount, planEvent.Count);
                testService.AssertEventNotQueued(ScriptingErrorEvent.Type);
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
                ScriptErrorParams errorEvent = await testService.Driver.WaitForEvent(ScriptingErrorEvent.Type, TimeSpan.FromMinutes(1));
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
                ScriptErrorParams errorEvent = await testService.Driver.WaitForEvent(ScriptingErrorEvent.Type, TimeSpan.FromMinutes(1));
                Assert.Equal("Invalid directory specified by the FilePath property.", errorEvent.Message);
            }
        }
    }
}
