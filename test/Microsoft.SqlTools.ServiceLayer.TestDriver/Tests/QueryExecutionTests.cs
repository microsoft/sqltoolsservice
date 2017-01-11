//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    public class QueryExecutionTests
    {
        /* Commenting out these tests until they are fixed (12/1/16)
        [Fact]
        public async Task TestQueryCancelReliability()
        {
            const string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Run and cancel 100 queries
                for (int i = 0; i < 100; i++)
                {
                    var queryTask = testHelper.RunQuery(queryTempFile.FilePath, query);

                    var cancelResult = await testHelper.CancelQuery(queryTempFile.FilePath);
                    Assert.NotNull(cancelResult);
                    Assert.True(string.IsNullOrEmpty(cancelResult.Messages));

                    await queryTask;
                }

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task TestQueryDoesNotBlockOtherRequests()
        {
            const string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Start a long-running query
                var queryTask = testHelper.RunQuery(queryTempFile.FilePath, query, 60000);

                // Interact with the service. None of these requests should time out while waiting for the query to finish
                for (int i = 0; i < 10; i++)
                {
                    using (SelfCleaningTempFile queryFile2 = new SelfCleaningTempFile())
                    {
                        await testHelper.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                        Assert.NotNull(await testHelper.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10));
                        await testHelper.Disconnect(queryFile2.FilePath);
                    }
                }

                await testHelper.CancelQuery(queryTempFile.FilePath);
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task TestParallelQueryExecution()
        {
            const int queryCount = 10;
            const string query = "SELECT * FROM sys.objects";

            using (TestHelper testHelper = new TestHelper())
            {
                // Create n connections
                SelfCleaningTempFile[] ownerUris = new SelfCleaningTempFile[queryCount];
                for (int i = 0; i < queryCount; i++)
                {
                    ownerUris[i] = new SelfCleaningTempFile();
                    Assert.NotNull(await testHelper.Connect(ownerUris[i].FilePath, ConnectionTestUtils.AzureTestServerConnection));
                }

                // Run n queries at once
                var queryTasks = new Task<QueryExecuteCompleteParams>[queryCount];
                for (int i = 0; i < queryCount; i++)
                {
                    queryTasks[i] = testHelper.RunQuery(ownerUris[i].FilePath, query);
                }
                await Task.WhenAll(queryTasks);

                // Verify that they all completed with results and Disconnect
                for (int i = 0; i < queryCount; i++)
                {
                    Assert.NotNull(queryTasks[i].Result);
                    Assert.NotNull(queryTasks[i].Result.BatchSummaries);
                    await testHelper.Disconnect(ownerUris[i].FilePath);
                    ownerUris[i].Dispose();
                }
            }
        }

        [Fact]
        public async Task TestSaveResultsDoesNotBlockOtherRequests()
        {
            const string query = "SELECT * FROM sys.objects";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Execute a query
                await testHelper.RunQuery(queryTempFile.FilePath, query);

                // Spawn several tasks to save results
                var saveTasks = new Task<SaveResultRequestResult>[100];
                for (int i = 0; i < 100; i++)
                {
                    if (i % 2 == 0)
                    {
                        saveTasks[i] = testHelper.SaveAsCsv(queryTempFile.FilePath, System.IO.Path.GetTempFileName(), 0, 0);
                    }
                    else
                    {
                        saveTasks[i] = testHelper.SaveAsJson(queryTempFile.FilePath, System.IO.Path.GetTempFileName(), 0, 0);
                    }
                }

                // Interact with the service. None of these requests should time out while waiting for the save results tasks to finish
                for (int i = 0; i < 10; i++)
                {
                    using(SelfCleaningTempFile queryFile2 = new SelfCleaningTempFile())
                    {
                        await testHelper.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                        Assert.NotNull(await testHelper.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10));
                        await testHelper.Disconnect(queryFile2.FilePath);
                    }
                }

                await Task.WhenAll(saveTasks);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task TestQueryingSubsetDoesNotBlockOtherRequests()
        {
            const string query = "SELECT * FROM sys.objects";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Execute a query
                await testHelper.RunQuery(queryTempFile.FilePath, query);

                // Spawn several tasks for subset requests
                var subsetTasks = new Task<QueryExecuteSubsetResult>[100];
                for (int i = 0; i < 100; i++)
                {
                    subsetTasks[i] = testHelper.ExecuteSubset(queryTempFile.FilePath, 0, 0, 0, 100);
                }

                // Interact with the service. None of these requests should time out while waiting for the subset tasks to finish
                for (int i = 0; i < 10; i++)
                {
                    using (SelfCleaningTempFile queryFile2 = new SelfCleaningTempFile())
                    {
                        await testHelper.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                        Assert.NotNull(await testHelper.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10));
                        await testHelper.Disconnect(queryFile2.FilePath);
                    }
                }

                await Task.WhenAll(subsetTasks);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task TestCancelQueryWhileOtherOperationsAreInProgress()
        {
            const string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                List<Task> tasks = new List<Task>();

                await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Execute a long-running query
                var queryTask = testHelper.RunQuery(queryTempFile.FilePath, query, 60000);

                // Queue up some tasks that interact with the service
                for (int i = 0; i < 10; i++)
                {
                    using (SelfCleaningTempFile queryFile2 = new SelfCleaningTempFile())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await testHelper.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                            await testHelper.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10);
                            await testHelper.RunQuery(queryFile2.FilePath, "SELECT * FROM sys.objects");
                            await testHelper.Disconnect(queryFile2.FilePath);
                        }));
                    }
                }

                // Cancel the long-running query
                await testHelper.CancelQuery(queryTempFile.FilePath);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task ExecuteBasicQueryTest()
        {
            const string query = "SELECT * FROM sys.all_columns c";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                bool connected = await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(500);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = queryTempFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testHelper.RequestOpenDocumentNotification(openParams);

                var queryResult = await testHelper.RunQuery(queryTempFile.FilePath, query, 10000);

                Assert.NotNull(queryResult);
                Assert.NotNull(queryResult.BatchSummaries);

                foreach (var batchSummary in queryResult.BatchSummaries)
                {
                    foreach (var resultSetSummary in batchSummary.ResultSetSummaries)
                    {
                        Assert.True(resultSetSummary.RowCount > 0);
                    }
                }

                var subsetRequest = new QueryExecuteSubsetParams()
                {
                    OwnerUri = queryTempFile.FilePath,
                    BatchIndex = 0,
                    ResultSetIndex = 0,
                    RowsStartIndex = 0,
                    RowsCount = 100,
                };
                
                var querySubset = await testHelper.RequestQueryExecuteSubset(subsetRequest);
                Assert.NotNull(querySubset);
                Assert.True(querySubset.ResultSubset.RowCount == 100);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task TestQueryingAfterCompletionRequests()
        {
            const string query = "SELECT * FROM sys.objects";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                List<Task> tasks = new List<Task>();

                await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(testHelper.RequestCompletion(queryTempFile.FilePath, query, 0, 10)));
                var queryTask = testHelper.RunQuery(queryTempFile.FilePath, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
               
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);
            
                await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.DataToolsTelemetryAzureConnection);
                tasks.Clear();
                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(testHelper.RequestCompletion(queryTempFile.FilePath, query, 0, 10)));
                queryTask = testHelper.RunQuery(queryTempFile.FilePath, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
            
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);

                await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.SqlDataToolsAzureConnection);
                tasks.Clear();
                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(testHelper.RequestCompletion(queryTempFile.FilePath, query, 0, 10)));
                queryTask = testHelper.RunQuery(queryTempFile.FilePath, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
            
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }
        */

        [Theory]
        [InlineData("-- no-op")]
        [InlineData("GO")]
        [InlineData("GO -- no-op")]
        public async Task NoOpQueryReturnsMessage(string query)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                Assert.True(await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.LocalhostConnection));

                // If: the query is executed...
                var queryResult = await testHelper.RunQueryAsync(queryTempFile.FilePath, query);
                var message = await testHelper.WaitForMessage();

                // Then:
                // ... I expect a query result to indicate successfully started query
                Assert.NotNull(queryResult);

                // ... I expect a non-error message to be returned without a batch associated with it
                Assert.NotNull(message);
                Assert.NotNull(message.Message);
                Assert.NotNull(message.Message.Message);
                Assert.False(message.Message.IsError);
                Assert.Null(message.Message.BatchId);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }
    }
}
