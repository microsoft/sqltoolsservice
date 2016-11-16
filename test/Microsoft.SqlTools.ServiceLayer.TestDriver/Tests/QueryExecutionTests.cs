//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    public class QueryExecutionTests
    {
       [Fact]
        public async Task TestQueryCancelReliability()
        {
            const string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c";

            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Run and cancel 100 queries
                for (int i = 0; i < 100; i++)
                {
                    var queryTask = testBase.RunQuery(queryFile.FilePath, query);

                    var cancelResult = await testBase.CancelQuery(queryFile.FilePath);
                    Assert.NotNull(cancelResult);
                    Assert.True(string.IsNullOrEmpty(cancelResult.Messages));

                    await queryTask;
                }

                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task TestQueryDoesNotBlockOtherRequests()
        {
            const string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c";

            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Start a long-running query
                var queryTask = testBase.RunQuery(queryFile.FilePath, query, 60000);

                // Interact with the service. None of these requests should time out while waiting for the query to finish
                for (int i = 0; i < 10; i++)
                {
                    using (SelfCleaningFile queryFile2 = new SelfCleaningFile())
                    {
                        await testBase.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                        Assert.NotNull(await testBase.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10));
                        await testBase.Disconnect(queryFile2.FilePath);
                    }
                }

                await testBase.CancelQuery(queryFile.FilePath);
                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task TestParallelQueryExecution()
        {
            const int queryCount = 10;
            const string query = "SELECT * FROM sys.objects";

            using (TestBase testBase = new TestBase())
            {
                // Create n connections
                SelfCleaningFile[] ownerUris = new SelfCleaningFile[queryCount];
                for (int i = 0; i < queryCount; i++)
                {
                    ownerUris[i] = new SelfCleaningFile();
                    Assert.NotNull(await testBase.Connect(ownerUris[i].FilePath, ConnectionTestUtils.AzureTestServerConnection));
                }

                // Run n queries at once
                var queryTasks = new Task<QueryExecuteCompleteParams>[queryCount];
                for (int i = 0; i < queryCount; i++)
                {
                    queryTasks[i] = testBase.RunQuery(ownerUris[i].FilePath, query);
                }
                await Task.WhenAll(queryTasks);

                // Verify that they all completed with results and Disconnect
                for (int i = 0; i < queryCount; i++)
                {
                    Assert.NotNull(queryTasks[i].Result);
                    Assert.NotNull(queryTasks[i].Result.BatchSummaries);
                    await testBase.Disconnect(ownerUris[i].FilePath);
                    ownerUris[i].Dispose();
                }
            }
        }

        [Fact]
        public async Task TestSaveResultsDoesNotBlockOtherRequests()
        {
            const string query = "SELECT * FROM sys.objects";

            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Execute a query
                await testBase.RunQuery(queryFile.FilePath, query);

                // Spawn several tasks to save results
                var saveTasks = new Task<SaveResultRequestResult>[100];
                for (int i = 0; i < 100; i++)
                {
                    if (i % 2 == 0)
                    {
                        saveTasks[i] = testBase.SaveAsCsv(queryFile.FilePath, System.IO.Path.GetTempFileName(), 0, 0);
                    }
                    else
                    {
                        saveTasks[i] = testBase.SaveAsJson(queryFile.FilePath, System.IO.Path.GetTempFileName(), 0, 0);
                    }
                }

                // Interact with the service. None of these requests should time out while waiting for the save results tasks to finish
                for (int i = 0; i < 10; i++)
                {
                    using(SelfCleaningFile queryFile2 = new SelfCleaningFile())
                    {
                        await testBase.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                        Assert.NotNull(await testBase.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10));
                        await testBase.Disconnect(queryFile2.FilePath);
                    }
                }

                await Task.WhenAll(saveTasks);

                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task TestQueryingSubsetDoesNotBlockOtherRequests()
        {
            const string query = "SELECT * FROM sys.objects";

            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Execute a query
                await testBase.RunQuery(queryFile.FilePath, query);

                // Spawn several tasks for subset requests
                var subsetTasks = new Task<QueryExecuteSubsetResult>[100];
                for (int i = 0; i < 100; i++)
                {
                    subsetTasks[i] = testBase.ExecuteSubset(queryFile.FilePath, 0, 0, 0, 100);
                }

                // Interact with the service. None of these requests should time out while waiting for the subset tasks to finish
                for (int i = 0; i < 10; i++)
                {
                    using (SelfCleaningFile queryFile2 = new SelfCleaningFile())
                    {
                        await testBase.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                        Assert.NotNull(await testBase.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10));
                        await testBase.Disconnect(queryFile2.FilePath);
                    }
                }

                await Task.WhenAll(subsetTasks);

                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task TestCancelQueryWhileOtherOperationsAreInProgress()
        {
            const string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b";

            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                List<Task> tasks = new List<Task>();

                await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                // Execute a long-running query
                var queryTask = testBase.RunQuery(queryFile.FilePath, query, 60000);

                // Queue up some tasks that interact with the service
                for (int i = 0; i < 10; i++)
                {
                    using (SelfCleaningFile queryFile2 = new SelfCleaningFile())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await testBase.Connect(queryFile2.FilePath, ConnectionTestUtils.AzureTestServerConnection);
                            await testBase.RequestCompletion(queryFile2.FilePath, "SELECT * FROM sys.objects", 0, 10);
                            await testBase.RunQuery(queryFile2.FilePath, "SELECT * FROM sys.objects");
                            await testBase.Disconnect(queryFile2.FilePath);
                        }));
                    }
                }

                // Cancel the long-running query
                await testBase.CancelQuery(queryFile.FilePath);

                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task ExecuteBasicQueryTest()
        {
            const string query = "SELECT * FROM sys.all_columns c";

            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                bool connected = await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(500);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = queryFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testBase.RequestOpenDocumentNotification(openParams);

                var queryResult = await testBase.RunQuery(queryFile.FilePath, query, 10000);

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
                    OwnerUri = queryFile.FilePath,
                    BatchIndex = 0,
                    ResultSetIndex = 0,
                    RowsStartIndex = 0,
                    RowsCount = 100,
                };
                
                var querySubset = await testBase.RequestQueryExecuteSubset(subsetRequest);
                Assert.NotNull(querySubset);
                Assert.True(querySubset.ResultSubset.RowCount == 100);

                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task TestQueryingAfterCompletionRequests()
        {
            const string query = "SELECT * FROM sys.objects";

            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                List<Task> tasks = new List<Task>();

                await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.AzureTestServerConnection);

                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(testBase.RequestCompletion(queryFile.FilePath, query, 0, 10)));
                var queryTask = testBase.RunQuery(queryFile.FilePath, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
               
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);
            
                await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.DataToolsTelemetryAzureConnection);
                tasks.Clear();
                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(testBase.RequestCompletion(queryFile.FilePath, query, 0, 10)));
                queryTask = testBase.RunQuery(queryFile.FilePath, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
            
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);

                await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.SqlDataToolsAzureConnection);
                tasks.Clear();
                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(testBase.RequestCompletion(queryFile.FilePath, query, 0, 10)));
                queryTask = testBase.RunQuery(queryFile.FilePath, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
            
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);

                await testBase.Disconnect(queryFile.FilePath);
            }
        }
    }
}
