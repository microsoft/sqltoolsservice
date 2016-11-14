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
    public class QueryExecutionTests : TestBase
    {
       [Fact]
        public async Task TestQueryCancelReliability()
        {
            string ownerUri = System.IO.Path.GetTempFileName();
            string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c";

            await Connect(ownerUri, ConnectionTestUtils.AzureTestServerConnection);

            // Run and cancel 100 queries
            for (int i = 0; i < 100; i++) 
            {
                var queryTask = RunQuery(ownerUri, query);

                var cancelResult = await CancelQuery(ownerUri);
                Assert.NotNull(cancelResult);
                Assert.True(string.IsNullOrEmpty(cancelResult.Messages));

                await queryTask;
            }
            
            await Disconnect(ownerUri);
        }

        [Fact]
        public async Task TestQueryDoesNotBlockOtherRequests()
        {
            string ownerUri = System.IO.Path.GetTempFileName();
            string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c";

            await Connect(ownerUri, ConnectionTestUtils.AzureTestServerConnection);

            // Start a long-running query
            var queryTask = RunQuery(ownerUri, query, 60000);

            // Interact with the service. None of these requests should time out while waiting for the query to finish
            for (int i = 0; i < 10; i++)
            {
                string ownerUri2 = System.IO.Path.GetTempFileName();

                await Connect(ownerUri2, ConnectionTestUtils.AzureTestServerConnection);
                Assert.NotNull(await RequestCompletion(ownerUri2, "SELECT * FROM sys.objects", 0, 10));
                await Disconnect(ownerUri2);
            }

            await CancelQuery(ownerUri);
            await Disconnect(ownerUri);
        }

        [Fact]
        public async Task TestParallelQueryExecution()
        {
            int queryCount = 10;

            // Create n connections
            string[] ownerUris = new string[queryCount];
            for (int i = 0; i < queryCount; i++)
            {
                ownerUris[i] = System.IO.Path.GetTempFileName();
                Assert.NotNull(await Connect(ownerUris[i], ConnectionTestUtils.AzureTestServerConnection));
            }

            // Run n queries at once
            string query = "SELECT * FROM sys.objects";
            var queryTasks = new Task<QueryExecuteCompleteParams>[queryCount];
            for (int i = 0; i < queryCount; i++)
            {
                queryTasks[i] = RunQuery(ownerUris[i], query);
            }
            await Task.WhenAll(queryTasks);

            // Verify that they all completed with results and Disconnect
            for (int i = 0; i < queryCount; i++)
            {
                Assert.NotNull(queryTasks[i].Result);
                Assert.NotNull(queryTasks[i].Result.BatchSummaries);
                await Disconnect(ownerUris[i]);
            }
        }

        [Fact]
        public async Task TestSaveResultsDoesNotBlockOtherRequests()
        {
            string ownerUri = System.IO.Path.GetTempFileName();
            string query = "SELECT * FROM sys.objects";

            await Connect(ownerUri, ConnectionTestUtils.AzureTestServerConnection);

            // Execute a query
            await RunQuery(ownerUri, query);

            // Spawn several tasks to save results
            var saveTasks = new Task<SaveResultRequestResult>[100];
            for (int i = 0; i < 100; i++)
            {
                if (i % 2 == 0)
                {
                    saveTasks[i] = SaveAsCsv(ownerUri, System.IO.Path.GetTempFileName(), 0, 0);
                }
                else
                {
                    saveTasks[i] = SaveAsJson(ownerUri, System.IO.Path.GetTempFileName(), 0, 0);
                }
            }

            // Interact with the service. None of these requests should time out while waiting for the save results tasks to finish
            for (int i = 0; i < 10; i++)
            {
                string ownerUri2 = System.IO.Path.GetTempFileName();

                await Connect(ownerUri2, ConnectionTestUtils.AzureTestServerConnection);
                Assert.NotNull(await RequestCompletion(ownerUri2, "SELECT * FROM sys.objects", 0, 10));
                await Disconnect(ownerUri2);
            }

            await Task.WhenAll(saveTasks);

            await Disconnect(ownerUri);
        }

        [Fact]
        public async Task TestQueryingSubsetDoesNotBlockOtherRequests()
        {
            string ownerUri = System.IO.Path.GetTempFileName();
            string query = "SELECT * FROM sys.objects";

            await Connect(ownerUri, ConnectionTestUtils.AzureTestServerConnection);

            // Execute a query
            await RunQuery(ownerUri, query);

            // Spawn several tasks for subset requests
            var subsetTasks = new Task<QueryExecuteSubsetResult>[100];
            for (int i = 0; i < 100; i++)
            {
                subsetTasks[i] = ExecuteSubset(ownerUri, 0, 0, 0, 100);
            }

            // Interact with the service. None of these requests should time out while waiting for the subset tasks to finish
            for (int i = 0; i < 10; i++)
            {
                string ownerUri2 = System.IO.Path.GetTempFileName();

                await Connect(ownerUri2, ConnectionTestUtils.AzureTestServerConnection);
                Assert.NotNull(await RequestCompletion(ownerUri2, "SELECT * FROM sys.objects", 0, 10));
                await Disconnect(ownerUri2);
            }

            await Task.WhenAll(subsetTasks);

            await Disconnect(ownerUri);
        }

        [Fact]
        public async Task TestCancelQueryWhileOtherOperationsAreInProgress()
        {
            string ownerUri = System.IO.Path.GetTempFileName();
            string query = "SELECT * FROM sys.objects a CROSS JOIN sys.objects b";
            List<Task> tasks = new List<Task>();

            await Connect(ownerUri, ConnectionTestUtils.AzureTestServerConnection);

            // Execute a long-running query
            var queryTask = RunQuery(ownerUri, query, 60000);

            // Queue up some tasks that interact with the service
            for (int i = 0; i < 10; i++)
            {
                string ownerUri2 = System.IO.Path.GetTempFileName();

                tasks.Add(Task.Run(async () =>
                {
                    await Connect(ownerUri2, ConnectionTestUtils.AzureTestServerConnection);
                    await RequestCompletion(ownerUri2, "SELECT * FROM sys.objects", 0, 10);
                    await RunQuery(ownerUri2, "SELECT * FROM sys.objects");
                    await Disconnect(ownerUri2);
                }));
            }

            // Cancel the long-running query
            await CancelQuery(ownerUri);

            await Disconnect(ownerUri);
        }

        [Fact]
        public async Task ExecuteBasicQueryTest()
        {
            try
            {
                string ownerUri = System.IO.Path.GetTempFileName();
                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(500);

                string query = "SELECT * FROM sys.all_columns c";

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = ownerUri,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await RequestOpenDocumentNotification(openParams);

                var queryResult = await RunQuery(ownerUri, query, 10000);

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
                    OwnerUri = ownerUri,
                    BatchIndex = 0,
                    ResultSetIndex = 0,
                    RowsStartIndex = 0,
                    RowsCount = 100,
                };
                
                var querySubset = await RequestQueryExecuteSubset(subsetRequest);
                Assert.NotNull(querySubset);
                Assert.True(querySubset.ResultSubset.RowCount == 100);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task TestQueryingAfterCompletionRequests()
        {
            try
            {
                string ownerUri = System.IO.Path.GetTempFileName();
                string query = "SELECT * FROM sys.objects";
                List<Task> tasks = new List<Task>();

                await Connect(ownerUri, ConnectionTestUtils.AzureTestServerConnection);

                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(RequestCompletion(ownerUri, query, 0, 10)));
                var queryTask = RunQuery(ownerUri, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
               
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);
            
                await Connect(ownerUri, ConnectionTestUtils.DataToolsTelemetryAzureConnection);
                tasks.Clear();
                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(RequestCompletion(ownerUri, query, 0, 10)));
                queryTask = RunQuery(ownerUri, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
            
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);

                await Connect(ownerUri, ConnectionTestUtils.SqlDataToolsAzureConnection);
                tasks.Clear();
                Enumerable.Range(0, 10).ToList().ForEach(arg => tasks.Add(RequestCompletion(ownerUri, query, 0, 10)));
                queryTask = RunQuery(ownerUri, query);
                tasks.Add(queryTask);
                await Task.WhenAll(tasks);
            
                Assert.NotNull(queryTask.Result);
                Assert.NotNull(queryTask.Result.BatchSummaries);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }
    }
}
