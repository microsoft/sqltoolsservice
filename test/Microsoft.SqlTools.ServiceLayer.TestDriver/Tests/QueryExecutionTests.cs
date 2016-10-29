//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    public class QueryExecutionTests : TestBase
    {
        [Fact]
        public async Task ExecuteBasicQueryTest()
        {
            try
            {
                string ownerUri = System.IO.Path.GetTempFileName();
                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(500);

                string query = "SELECT * FROM sys.objects";

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

                var queryResult = await RunQuery(ownerUri, query);

                Assert.NotNull(queryResult);
                Assert.NotNull(queryResult.BatchSummaries);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        //[Fact]
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
