//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;
using Range = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Range;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    [TestFixture]
    public class StressTests
    {
        /// <summary>
        /// Simulate typing by a user to stress test the language service
        /// </summary>
        //[Test]
        public async Task TestLanguageService()
        {
            const string textToType = "SELECT * FROM sys.objects GO " +
                                      "CREATE TABLE MyTable(" +
                                      "FirstName CHAR," +
                                      "LastName CHAR," +
                                      "DateOfBirth DATETIME," +
                                      "CONSTRAINT MyTableConstraint UNIQUE (FirstName, LastName, DateOfBirth)) GO " +
                                      "INSERT INTO MyTable (FirstName, LastName, DateOfBirth) VALUES ('John', 'Doe', '19800101') GO " +
                                      "SELECT * FROM MyTable GO " +
                                      "ALTER TABLE MyTable DROP CONSTRAINT MyTableConstraint GO " +
                                      "DROP TABLE MyTable GO ";


            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                // Connect
                bool connected = await testService.Connect(TestServerType.OnPrem, string.Empty, queryTempFile.FilePath);
                Assert.True(connected, "Connection was not successful");

                Thread.Sleep(10000); // Wait for intellisense to warm up

                // Simulate typing
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                int version = 1;
                while (stopwatch.Elapsed < TimeSpan.FromMinutes(60))
                {
                    for (int i = 0; i < textToType.Length; i++)
                    {
                        System.IO.File.WriteAllText(queryTempFile.FilePath, textToType.Substring(0, i + 1));

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
                                Uri = queryTempFile.FilePath
                            }
                        };

                        await testService.RequestChangeTextDocumentNotification(changeParams);

                        Thread.Sleep(50);

                        // If we just typed a space, request/resolve completion
                        if (textToType[i] == ' ')
                        {
                            var completions = await testService.RequestCompletion(queryTempFile.FilePath, textToType.Substring(0, i + 1), 0, i + 1);
                            Assert.True(completions != null && completions.Length > 0, "Completion items list was null or empty");

                            Thread.Sleep(50);

                            var item = await testService.RequestResolveCompletion(completions[0]);

                            Assert.NotNull(item);
                        }
                    }

                    // Clear the text document
                    System.IO.File.WriteAllText(queryTempFile.FilePath, "");

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
                            Uri = queryTempFile.FilePath
                        }
                    };

                    await testService.RequestChangeTextDocumentNotification(changeParams2);
                }

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Repeatedly execute queries to stress test the query execution service.
        /// </summary>
        //[Test]
        public async Task TestQueryExecutionService()
        {
            const string queryToRun = "SELECT * FROM sys.all_objects GO " +
                                      "SELECT * FROM sys.objects GO " +
                                      "SELECT * FROM sys.tables GO " +
                                      "SELECT COUNT(*) FROM sys.objects";

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                // Connect
                bool connected = await testService.Connect(TestServerType.OnPrem, string.Empty, queryTempFile.FilePath);
                Assert.True(connected, "Connection is successful");

                // Run queries repeatedly
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                while (stopwatch.Elapsed < TimeSpan.FromMinutes(60))
                {
                    var queryResult = await testService.RunQueryAndWaitToComplete(queryTempFile.FilePath, queryToRun, 10000);

                    Assert.NotNull(queryResult);
                    Assert.That(queryResult.BatchSummaries, Is.Not.Null, "queryResult.BatchSummaries");
                    Assert.That(queryResult.BatchSummaries.Select(b => b.ResultSetSummaries), Has.Exactly(4).Not.Null, "ResultSetSummaries in the queryResult");

                    Assert.NotNull(await testService.ExecuteSubset(queryTempFile.FilePath, 0, 0, 0, 7));
                    Assert.NotNull(await testService.ExecuteSubset(queryTempFile.FilePath, 1, 0, 0, 7));
                    Assert.NotNull(await testService.ExecuteSubset(queryTempFile.FilePath, 2, 0, 0, 7));
                    Assert.NotNull(await testService.ExecuteSubset(queryTempFile.FilePath, 3, 0, 0, 1));

                    Thread.Sleep(500);
                }

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Repeatedly connect and disconnect to stress test the connection service.
        /// </summary>
        //[Test]
        public async Task TestConnectionService()
        {
            string ownerUri = "file:///my/test/file.sql";


            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                var connection = testService.GetConnectionParameters(TestServerType.OnPrem);
                connection.Connection.Pooling = false;

                // Connect/disconnect repeatedly
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                while (stopwatch.Elapsed < TimeSpan.FromMinutes(60))
                {
                    // Connect
                    bool connected = await testService.Connect(ownerUri, connection);
                    Assert.True(connected, "Connection is successful");

                    // Disconnect
                    bool disconnected = await testService.Disconnect(ownerUri);
                    Assert.True(disconnected, "Disconnect is successful");
                }
            }
        }
    }
}
