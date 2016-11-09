//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    public class StressTests : TestBase
    {
        /// <summary>
        /// Simulate typing by a user to stress test the language service
        /// </summary>
        //[Fact]
        public async Task TestLanguageService()
        {
            string textToType = 
                "SELECT * FROM sys.objects GO " +
                "CREATE TABLE MyTable(" +
                "FirstName CHAR," +
                "LastName CHAR," +
                "DateOfBirth DATETIME," +
                "CONSTRAINT MyTableConstraint UNIQUE (FirstName, LastName, DateOfBirth)) GO " +
                "INSERT INTO MyTable (FirstName, LastName, DateOfBirth) VALUES ('John', 'Doe', '19800101') GO " +
                "SELECT * FROM MyTable GO " +
                "ALTER TABLE MyTable DROP CONSTRAINT MyTableConstraint GO " +
                "DROP TABLE MyTable GO ";
            var ownerUri = System.IO.Path.GetTempFileName();

            try
            {
                // Connect
                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(10000); // Wait for intellisense to warm up

                // Simulate typing
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                int version = 1;
                while (stopwatch.Elapsed < TimeSpan.FromMinutes(60))
                {
                    for (int i = 0; i < textToType.Length; i++)
                    {
                        System.IO.File.WriteAllText(ownerUri, textToType.Substring(0, i + 1));

                        var contentChanges = new TextDocumentChangeEvent[1];
                        contentChanges[0] = new TextDocumentChangeEvent()
                        {
                            Range = new Range()
                            {
                                Start = new Position()
                                {
                                    Line = 0,
                                    Character = i
                                },
                                End = new Position()
                                {
                                    Line = 0,
                                    Character = i
                                }
                            },
                            RangeLength = 1,
                            Text = textToType.Substring(i, 1)
                        };

                        DidChangeTextDocumentParams changeParams = new DidChangeTextDocumentParams()
                        {
                            ContentChanges = contentChanges,
                            TextDocument = new VersionedTextDocumentIdentifier()
                            {
                                Version = ++version,
                                Uri = ownerUri
                            }
                        };

                        await RequestChangeTextDocumentNotification(changeParams);

                        Thread.Sleep(50);

                        // If we just typed a space, request/resolve completion
                        if (textToType[i] == ' ')
                        {
                            var completions = await RequestCompletion(ownerUri, textToType.Substring(0, i + 1), 0, i + 1);
                            Assert.True(completions != null && completions.Length > 0, "Completion items list is not null and not empty");

                            Thread.Sleep(50);

                            var item = await RequestResolveCompletion(completions[0]);

                            Assert.NotNull(item);
                        }
                    }

                    // Clear the text document
                    System.IO.File.WriteAllText(ownerUri, "");

                    var contentChanges2 = new TextDocumentChangeEvent[1];
                    contentChanges2[0] = new TextDocumentChangeEvent()
                    {
                        Range = new Range()
                        {
                            Start = new Position()
                            {
                                Line = 0,
                                Character = 0
                            },
                            End = new Position()
                            {
                                Line = 0,
                                Character = textToType.Length - 1
                            }
                        },
                        RangeLength = textToType.Length,
                        Text = ""
                    };

                    DidChangeTextDocumentParams changeParams2 = new DidChangeTextDocumentParams()
                    {
                        ContentChanges = contentChanges2,
                        TextDocument = new VersionedTextDocumentIdentifier()
                        {
                            Version = ++version,
                            Uri = ownerUri
                        }
                    };

                    await RequestChangeTextDocumentNotification(changeParams2);
                }

                await Disconnect(ownerUri);
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(ownerUri);
                }
                catch {}
                WaitForExit();
            }
        }

        /// <summary>
        /// Repeatedly execute queries to stress test the query execution service.
        /// </summary>
        //[Fact]
        public async Task TestQueryExecutionService()
        {
            string queryToRun = "SELECT * FROM sys.all_objects GO SELECT * FROM sys.objects GO SELECT * FROM sys.tables GO SELECT COUNT(*) FROM sys.objects";
            var ownerUri = System.IO.Path.GetTempFileName();

            try
            {
                // Connect
                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                // Run queries repeatedly
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                while (stopwatch.Elapsed < TimeSpan.FromMinutes(60))
                {
                    var queryResult = await RunQuery(ownerUri, queryToRun, 10000);

                    Assert.NotNull(queryResult);
                    Assert.NotNull(queryResult.BatchSummaries);
                    Assert.NotEmpty(queryResult.BatchSummaries);
                    Assert.NotNull(queryResult.BatchSummaries[0].ResultSetSummaries);
                    Assert.NotNull(queryResult.BatchSummaries[1].ResultSetSummaries);
                    Assert.NotNull(queryResult.BatchSummaries[2].ResultSetSummaries);
                    Assert.NotNull(queryResult.BatchSummaries[3].ResultSetSummaries);

                    Assert.NotNull(await ExecuteSubset(ownerUri, 0, 0, 0, 7));
                    Assert.NotNull(await ExecuteSubset(ownerUri, 1, 0, 0, 7));
                    Assert.NotNull(await ExecuteSubset(ownerUri, 2, 0, 0, 7));
                    Assert.NotNull(await ExecuteSubset(ownerUri, 3, 0, 0, 1));

                    Thread.Sleep(500);
                }

                await Disconnect(ownerUri);
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(ownerUri);
                }
                catch {}
                WaitForExit();
            }
        }

        /// <summary>
        /// Repeatedly connect and disconnect to stress test the connection service.
        /// </summary>
        //[Fact]
        public async Task TestConnectionService()
        {
            string ownerUri = "file:///my/test/file.sql";

            var connection = ConnectionTestUtils.LocalhostConnection;
            connection.Connection.Pooling = false;

            try
            {
                // Connect/disconnect repeatedly
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                while (stopwatch.Elapsed < TimeSpan.FromMinutes(60))
                {
                    // Connect
                    bool connected = await Connect(ownerUri, connection);
                    Assert.True(connected, "Connection is successful");

                    // Disconnect
                    bool disconnected = await Disconnect(ownerUri);
                    Assert.True(disconnected, "Disconnect is successful");
                }
            }
            finally
            {
                WaitForExit();
            }
        }
    }
}
