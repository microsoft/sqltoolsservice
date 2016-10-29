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
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Language Service end-to-end integration tests
    /// </summary>
    public class LanguageServiceTests : TestBase
    {

        /// <summary>
        /// Validate hover tooltip scenarios
        /// </summary>
        [Fact]
        public async Task HoverTest()
        {
            try
            {
                string ownerUri = System.IO.Path.GetTempFileName();
                string query = "SELECT * FROM sys.objects";

                WriteToFile(ownerUri, query);

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
                  
                Thread.Sleep(500);

                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(10000);

                Hover hover = await RequestHover(ownerUri, query, 0, 15);

                Assert.True(hover != null, "Hover tooltop is not null");

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        /// <summary>
        /// Validation autocompletion suggestions scenarios
        /// </summary>
        [Fact]
        public async Task CompletionTest()
        {
            try
            {      
                string ownerUri = System.IO.Path.GetTempFileName();
                string query = "SELECT * FROM sys.objects";

                WriteToFile(ownerUri, query);

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
                  
                Thread.Sleep(500);

                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(10000);

                CompletionItem[] completions = await RequestCompletion(ownerUri, query, 0, 15);

                Assert.True(completions != null && completions.Length > 0, "Completion items list is not null and not empty");

                Thread.Sleep(50);

                CompletionItem item = await RequestResolveCompletion(completions[0]);

                Assert.True(completions != null && completions.Length > 0, "Completion items list is not null and not empty");

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        /// <summary>
        /// Validate diagnostic scenarios
        /// </summary>
        [Fact]
        public async Task DiagnosticsTests()
        {
            try
            {            
                string ownerUri = System.IO.Path.GetTempFileName();
                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(500);

                string query = "SELECT *** FROM sys.objects";

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
              
                Thread.Sleep(100);

                var contentChanges = new TextDocumentChangeEvent[1];
                contentChanges[0] = new TextDocumentChangeEvent()
                {
                    Range = new Range()
                    {
                        Start = new Position()
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position()
                        {
                            Line = 0,
                            Character = 6
                        }
                    },
                    RangeLength = 1,
                    Text = "z"
                };

                DidChangeTextDocumentParams changeParams = new DidChangeTextDocumentParams()
                {
                    ContentChanges = contentChanges,
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Version = 2,
                        Uri = ownerUri
                    }
                };

                await RequestChangeTextDocumentNotification(changeParams);

                Thread.Sleep(100);
        
                contentChanges[0] = new TextDocumentChangeEvent()
                {
                    Range = new Range()
                    {
                        Start = new Position()
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position()
                        {
                            Line = 0,
                            Character = 6
                        }
                    },
                    RangeLength = 1,
                    Text = "t"
                };

                changeParams = new DidChangeTextDocumentParams()
                {
                    ContentChanges = contentChanges,
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Version = 3,
                        Uri = ownerUri
                    }
                };

                await RequestChangeTextDocumentNotification(changeParams);

                Thread.Sleep(2500);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        /// <summary>
        /// Validate the configuration change event
        /// </summary>
        [Fact]
        public async Task ChangeConfigurationTest()
        {
            try
            {            
                string ownerUri = System.IO.Path.GetTempFileName();
                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(500);             

                var settings = new SqlToolsSettings();
                settings.SqlTools.IntelliSense.EnableIntellisense = false;
                DidChangeConfigurationParams<SqlToolsSettings> configParams = new DidChangeConfigurationParams<SqlToolsSettings>()
                {
                    Settings = settings
                };

                await RequestChangeConfigurationNotification(configParams);

                Thread.Sleep(2000);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

    }
}
