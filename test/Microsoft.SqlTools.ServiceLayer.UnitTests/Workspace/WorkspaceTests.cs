﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Workspace
{
    public class WorkspaceTests
    {
        [Test]
        public async Task FileClosedSuccessfully()
        {
            // Given:
            // ... A workspace that has a single file open
            var workspace = new ServiceLayer.Workspace.Workspace();
            var workspaceService = new WorkspaceService<SqlToolsSettings> {Workspace = workspace};
            var openedFile = workspace.GetFileBuffer(TestObjects.ScriptUri, string.Empty);
            Assert.NotNull(openedFile);
            Assert.That(workspace.GetOpenedFiles(), Is.Not.Empty);

            // ... And there is a callback registered for the file closed event
            ScriptFile closedFile = null;
            string closedUri = null;
            workspaceService.RegisterTextDocCloseCallback((u, f, c) =>
            {
                closedUri = u;
                closedFile = f;
                return Task.FromResult(true);
            });

            // If:
            // ... An event to close the open file occurs
            var eventContext = new Mock<EventContext>().Object;
            var requestParams = new DidCloseTextDocumentParams
            {
                TextDocument = new TextDocumentItem {Uri = TestObjects.ScriptUri}
            };
            await workspaceService.HandleDidCloseTextDocumentNotification(requestParams, eventContext);

            // Then:
            // ... The file should no longer be in the open files
            Assert.That(workspace.GetOpenedFiles(), Is.Empty);

            // ... The callback should have been called
            // ... The provided script file should be the one we created
            Assert.NotNull(closedFile);
            Assert.AreEqual(openedFile, closedFile);
            Assert.AreEqual(TestObjects.ScriptUri, closedUri);
        }

        [Test]
        public async Task FileClosedNotOpen()
        {
            // Given:
            // ... A workspace that has no files open
            var workspace = new ServiceLayer.Workspace.Workspace();
            var workspaceService = new WorkspaceService<SqlToolsSettings> {Workspace = workspace};
            Assert.That(workspace.GetOpenedFiles(), Is.Empty);

            // ... And there is a callback registered for the file closed event
            bool callbackCalled = false;
            workspaceService.RegisterTextDocCloseCallback((u, f, c) =>
            {
                callbackCalled = true;
                return Task.FromResult(true);
            });

            // If:
            // ... An event to close the a file occurs
            var eventContext = new Mock<EventContext>().Object;
            var requestParams = new DidCloseTextDocumentParams
            {
                TextDocument = new TextDocumentItem {Uri = TestObjects.ScriptUri}
            };
            // Then:
            await workspaceService.HandleDidCloseTextDocumentNotification(requestParams, eventContext);

            // ... There should still be no open files
            // ... The callback should not have been called
            Assert.That(workspace.GetOpenedFiles(), Is.Empty);
            Assert.False(callbackCalled);
        }

        [Test]
        public void BufferRangeNoneNotNull()
        {
            Assert.NotNull(BufferRange.None); 
        }

        [Test]
        public void BufferRangeStartGreaterThanEnd()
        {
            Assert.Throws<ArgumentException>(() => 
                new BufferRange(new BufferPosition(2, 2), new BufferPosition(1, 1)));
        }

        [Test]
        public void BufferRangeEquals()
        {
            var range = new BufferRange(new BufferPosition(1, 1), new BufferPosition(2, 2));
            Assert.False(range.Equals(null));
            Assert.True(range.Equals(range));
            Assert.NotNull(range.GetHashCode());
        }

        [Test]
        public void UnescapePath()
        {
            Assert.NotNull(Microsoft.SqlTools.ServiceLayer.Workspace.Workspace.UnescapePath("`/path/`"));
        }

        [Test]
        public void GetBaseFilePath()
        {
            RunIfWrapper.RunIfWindows(() => 
            {  
                using (var workspace = new ServiceLayer.Workspace.Workspace())
                {
                    Assert.Throws<InvalidOperationException>(() => workspace.GetBaseFilePath("path"));
                    Assert.NotNull(workspace.GetBaseFilePath(@"c:\path\file.sql"));
                    Assert.AreEqual(workspace.GetBaseFilePath("tsqloutput://c:/path/file.sql"), workspace.WorkspacePath);
                }
            });
        }

        [Test]
        public void ResolveRelativeScriptPath()
        {
            RunIfWrapper.RunIfWindows(() => 
            { 
                var workspace = new ServiceLayer.Workspace.Workspace();
                Assert.NotNull(workspace.ResolveRelativeScriptPath(null, @"c:\path\file.sql"));
                Assert.NotNull(workspace.ResolveRelativeScriptPath(@"c:\path\", "file.sql"));
            });
        }

        [Test]
        public async Task DontProcessGitFileEvents()
        {
            await VerifyFileIsNotAddedOnDocOpened("git:/myfile.sql");
        }

        [Test]
        public async Task DontProcessPerforceFileEvents()
        {
            await VerifyFileIsNotAddedOnDocOpened("perforce:/myfile.sql");
        }

        private async Task VerifyFileIsNotAddedOnDocOpened(string filePath)
        {
             // setup test workspace
            var workspace = new ServiceLayer.Workspace.Workspace();
            var workspaceService = new WorkspaceService<SqlToolsSettings> {Workspace = workspace};

            // send a document open event with git:/ prefix URI
            var openParams = new DidOpenTextDocumentNotification
            {
                TextDocument = new TextDocumentItem { Uri = filePath }
            };

            await workspaceService.HandleDidOpenTextDocumentNotification(openParams, eventContext: null);

            // verify the file is not being tracked by workspace
            Assert.False(workspaceService.Workspace.ContainsFile(filePath));

            // send a close event with git:/ prefix URI
            var closeParams = new DidCloseTextDocumentParams
            {
                TextDocument = new TextDocumentItem { Uri = filePath }
            };

            await workspaceService.HandleDidCloseTextDocumentNotification(closeParams, eventContext: null);

            // this is not that interesting validation since the open is ignored
            // the main validation is that close doesn't raise an exception
            Assert.False(workspaceService.Workspace.ContainsFile(filePath));
        }

        [Test]
        public void GetFileReturnsNullForPerforceFile()
        {
            // when I ask for a non-file object in the workspace, it should return null
            var workspace = new ServiceLayer.Workspace.Workspace();
            ScriptFile file = workspace.GetFile("perforce:myfile.sql");            
            Assert.Null(file);
        }

        [Test]
        [TestCase(TestObjects.ScriptUri)]
        [TestCase("file://some/path%20with%20encoded%20spaces/file.sql")]
        [TestCase("file://some/path with spaces/file.sql")]
        [TestCase("file://some/fileWith#.sql")]
        [TestCase("file://some/fileUriWithQuery.sql?var=foo")]
        [TestCase("file://some/fileUriWithFragment.sql#foo")]
        public async Task WorkspaceContainsFile(string uri)
        {
            var workspace = new ServiceLayer.Workspace.Workspace();
            var workspaceService = new WorkspaceService<SqlToolsSettings> {Workspace = workspace};
            workspace.GetFileBuffer(uri, string.Empty);

            // send a document open event            
            var openParams = new DidOpenTextDocumentNotification
            {
                TextDocument = new TextDocumentItem { Uri = uri }
            };

            await workspaceService.HandleDidOpenTextDocumentNotification(openParams, eventContext: null);

            // verify the file is being tracked by workspace
            Assert.True(workspaceService.Workspace.ContainsFile(uri));
        }

        [Test]
        public void DontBindToObjectExplorerConnectEvents()
        {
            // when I ask for a non-file object in the workspace, it should return null
            var workspace = new ServiceLayer.Workspace.Workspace();
            ScriptFile file = workspace.GetFile("objectexplorer://server;database=database;user=user");            
            Assert.Null(file);

            // when I ask for a file, it should return the file
            string tempFile = Path.GetTempFileName();
            string fileContents = "hello world";
            File.WriteAllText(tempFile, fileContents);

            file = workspace.GetFile(tempFile);
            Assert.AreEqual(fileContents, file.Contents);

            if (tempFile.StartsWith("/"))
            {
                tempFile = tempFile.Substring(1);
            }
            file = workspace.GetFile("file://" + tempFile);
            Assert.AreEqual(fileContents, file.Contents);

            file = workspace.GetFileBuffer("untitled://"+ tempFile, fileContents);
            Assert.AreEqual(fileContents, file.Contents);

            // For windows files, just check scheme is null since it's hard to mock file contents in these
            Assert.Null(ServiceLayer.Workspace.Workspace.GetScheme(@"C:\myfile.sql"));
            Assert.Null(ServiceLayer.Workspace.Workspace.GetScheme(@"\\myfile.sql"));
        }

    }
}
