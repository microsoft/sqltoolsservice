//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    public class QueryExecutionTests
    {
        /* Commenting out these tests until they are fixed (12/1/16)
        [Test]
        public async Task TestQueryCancelReliability()
        {
            const string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestService testService = new TestService())
            {
                await TestService.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Run and cancel 100 queries
                for (int i = 0; i < 100; i++)
                {
                    var queryTask = TestService.RunQuery(queryTempFile.FilePath, query);

                    var cancelResult = await TestService.CancelQuery(queryTempFile.FilePath);
                    Assert.NotNull(cancelResult);
                    Assert.True(string.IsNullOrEmpty(cancelResult.Messages));

                    await queryTask;
                }

                await TestService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Test]
        public async Task TestQueryDoesNotBlockOtherRequests()
        {
            const string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestService testService = new TestService())
            {
                await TestService.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Start a long-running query
                var queryTask = TestService.RunQuery(queryTempFile.FilePath, query, 60000);

                // Interact with the service. None of these requests should time out while waiting for the query to finish
                for (int i = 0; i < 10; i++)
                {
                    using (SelfCleaningTempFile queryFile2 = new SelfCleaningTempFile())
                    {
                        await TestService.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                        Assert.NotNull(await TestService.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10));
                        await TestService.Disconnect(queryFile2.FilePath);
                    }
                }

                await TestService.CancelQuery(queryTempFile.FilePath);
                await TestService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Test]
        public async Task TestParallelQueryExecution()
        {
            const int queryCount = 10;
            const string query = "SELECT * FROM sys.objects";

            using (TestService testService = new TestService())
            {
                // Create n connections
                SelfCleaningTempFile[] ownerUris = new SelfCleaningTempFile[queryCount];
                for (int i = 0; i < queryCount; i++)
                {
                    ownerUris[i] = new SelfCleaningTempFile();
                    Assert.NotNull(await TestService.Connect(ownerUris[i].FilePath, ConnectionTestUtils.AzureTestServerConnection));
                }

                // Run n queries at once
                var queryTasks = new Task<QueryCompleteParams>[queryCount];
                for (int i = 0; i < queryCount; i++)
                {
                    queryTasks[i] = TestService.RunQuery(ownerUris[i].FilePath, query);
                }
                await Task.WhenAll(queryTasks);

                // Verify that they all completed with results and Disconnect
                for (int i = 0; i < queryCount; i++)
                {
                    Assert.NotNull(queryTasks[i].Result);
                    Assert.NotNull(queryTasks[i].Result.BatchSummaries);
                    await TestService.Disconnect(ownerUris[i].FilePath);
                    ownerUris[i].Dispose();
                }
            }
        }

        [Test]
        public async Task TestSaveResultsDoesNotBlockOtherRequests()
        {
            const string query = "SELECT * FROM sys.objects";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestService testService = new TestService())
            {
                await TestService.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Execute a query
                await TestService.RunQuery(queryTempFile.FilePath, query);

                // Spawn several tasks to save results
                var saveTasks = new Task<SaveResultRequestResult>[100];
                for (int i = 0; i < 100; i++)
                {
                    if (i % 2 == 0)
                    {
                        saveTasks[i] = TestService.SaveAsCsv(queryTempFile.FilePath, System.IO.Path.GetTempFileName(), 0, 0);
                    }
                    else
                    {
                        saveTasks[i] = TestService.SaveAsJson(queryTempFile.FilePath, System.IO.Path.GetTempFileName(), 0, 0);
                    }
                }

                // Interact with the service. None of these requests should time out while waiting for the save results tasks to finish
                for (int i = 0; i < 10; i++)
                {
                    using(SelfCleaningTempFile queryFile2 = new SelfCleaningTempFile())
                    {
                        await TestService.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                        Assert.NotNull(await TestService.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10));
                        await TestService.Disconnect(queryFile2.FilePath);
                    }
                }

                await Task.WhenAll(saveTasks);

                await TestService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Test]
        public async Task TestQueryingSubsetDoesNotBlockOtherRequests()
        {
            const string query = "SELECT * FROM sys.objects";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestService testService = new TestService())
            {
                await TestService.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Execute a query
                await TestService.RunQuery(queryTempFile.FilePath, query);

                // Spawn several tasks for subset requests
                var subsetTasks = new Task<SubsetResult>[100];
                for (int i = 0; i < 100; i++)
                {
                    subsetTasks[i] = TestService.ExecuteSubset(queryTempFile.FilePath, 0, 0, 0, 100);
                }

                // Interact with the service. None of these requests should time out while waiting for the subset tasks to finish
                for (int i = 0; i < 10; i++)
                {
                    using (SelfCleaningTempFile queryFile2 = new SelfCleaningTempFile())
                    {
                        await TestService.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                        Assert.NotNull(await TestService.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10));
                        await TestService.Disconnect(queryFile2.FilePath);
                    }
                }

                await Task.WhenAll(subsetTasks);

                await TestService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Test]
        public async Task TestCancelQueryWhileOtherOperationsAreInProgress()
        {
            const string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestService testService = new TestService())
            {
                List<Task> tasks = new List<Task>();

                await TestService.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Execute a long-running query
                var queryTask = TestService.RunQuery(queryTempFile.FilePath, query, 60000);

                // Queue up some tasks that interact with the service
                for (int i = 0; i < 10; i++)
                {
                    using (SelfCleaningTempFile queryFile2 = new SelfCleaningTempFile())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await TestService.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                            await TestService.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10);
                            await TestService.RunQuery(queryFile2.FilePath, "SELECT * FROM sys.objects");
                            await TestService.Disconnect(queryFile2.FilePath);
                        }));
                    }
                }

                // Cancel the long-running query
                await TestService.CancelQuery(queryTempFile.FilePath);

                await TestService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Test]
        public async Task ExecuteBasicQueryTest()
        {
            const string query = "SELECT * FROM sys.all_columns c";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestService testService = new TestService())
            {
                bool connected = await TestService.Connect(TestServerType.OnPrem, string.Empty, queryTempFile.FilePath);
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

                await TestService.RequestOpenDocumentNotification(openParams);

                var queryResult = await TestService.RunQuery(queryTempFile.FilePath, query, 10000);

                Assert.NotNull(queryResult);
                Assert.NotNull(queryResult.BatchSummaries);

                foreach (var batchSummary in queryResult.BatchSummaries)
                {
                    foreach (var resultSetSummary in batchSummary.ResultSetSummaries)
                    {
                        Assert.True(resultSetSummary.RowCount > 0);
                    }
                }

                var subsetRequest = new SubsetParams()
                {
                    OwnerUri = queryTempFile.FilePath,
                    BatchIndex = 0,
                    ResultSetIndex = 0,
                    RowsStartIndex = 0,
                    RowsCount = 100,
                };
                
                var querySubset = await TestService.RequestQueryExecuteSubset(subsetRequest);
                Assert.NotNull(querySubset);
                Assert.True(querySubset.ResultSubset.RowCount == 100);

                await TestService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Test]
        public async Task TestQueryingAfterCompletionRequests()
        {
            const string query = "SELECT * FROM sys.objects";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestService testService = new TestService())
            {
                List<Task> tasks = new List<Task>();

                await TestService.Connect(queryTempFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(TestService.RequestCompletion(queryTempFile.FilePath, query, 0, 10)));
                var queryTask = TestService.RunQuery(queryTempFile.FilePath, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
               
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);
            
                await TestService.Connect(queryTempFile.FilePath, ConnectionTestUtils.DataToolsTelemetryAzureConnection);
                tasks.Clear();
                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(TestService.RequestCompletion(queryTempFile.FilePath, query, 0, 10)));
                queryTask = TestService.RunQuery(queryTempFile.FilePath, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
            
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);

                await TestService.Connect(queryTempFile.FilePath, ConnectionTestUtils.SqlDataToolsAzureConnection);
                tasks.Clear();
                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(TestService.RequestCompletion(queryTempFile.FilePath, query, 0, 10)));
                queryTask = TestService.RunQuery(queryTempFile.FilePath, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
            
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);

                await TestService.Disconnect(queryTempFile.FilePath);
            }
        }
        

        [Theory]
        [InlineData("-- no-op")]
        [InlineData("GO")]
        [InlineData("GO -- no-op")]
        public async Task NoOpQueryReturnsMessage(string query)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {

                Assert.True(await testService.Connect(TestServerType.OnPrem, queryTempFile.FilePath));
                // If: the no-op query is executed...
                var queryResult = await testService.RunQueryAsync(queryTempFile.FilePath, query);
                var message = await testService.WaitForMessage();

                // Then:
                // ... I expect a query result to indicate successfully started query
                Assert.NotNull(queryResult);

                // ... I expect a non-error message to be returned without a batch associated with it
                Assert.NotNull(message);
                Assert.NotNull(message.Message);
                Assert.NotNull(message.Message.Message);
                Assert.False(message.Message.IsError);
                Assert.Null(message.Message.BatchId);

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }
        */
    }
}
