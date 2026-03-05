//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices;
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

        #region Windows File Path Tests

        [Test]
        public void GetFile_FileUriWithEncodedColon_ResolvesCorrectly()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";
                // Open with encoded colon (%3A) in drive letter
                var file = workspace.GetFileBuffer("file:///c%3A/Users/dev/query.sql", contents);
                Assert.NotNull(file);
                Assert.AreEqual(contents, file.Contents);
            });
        }

        [Test]
        public void GetFile_FileUriWithNormalColon_ResolvesCorrectly()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";
                var file = workspace.GetFileBuffer("file:///C:/Users/dev/query.sql", contents);
                Assert.NotNull(file);
                Assert.AreEqual(contents, file.Contents);
            });
        }

        [Test]
        public void GetFile_EncodedAndNormalColon_ResolveSameKey()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";

                // Open with encoded colon
                workspace.GetFileBuffer("file:///c%3A/Users/dev/query.sql", contents);

                // The same file opened with normal colon should resolve to the same entry
                Assert.True(workspace.ContainsFile("file:///c:/Users/dev/query.sql"));
            });
        }

        [Test]
        public void GetFile_UntitledScheme_ReturnsBufferContents()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";
                var file = workspace.GetFileBuffer("untitled:Untitled-1", contents);
                Assert.NotNull(file);
                Assert.AreEqual(contents, file.Contents);
            });
        }

        [Test]
        public void GetFile_UntitledScheme_GetFileReturnsNull()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                // GetFile (not GetFileBuffer) should return null for untitled files
                // that haven't been opened via GetFileBuffer
                var workspace = new ServiceLayer.Workspace.Workspace();
                var file = workspace.GetFile("untitled:Untitled-1");
                Assert.Null(file);
            });
        }

        [Test]
        public void GetFile_GitScheme_ReturnsNull()
        {
            var workspace = new ServiceLayer.Workspace.Workspace();
            var file = workspace.GetFile("git:/myfile.sql");
            Assert.Null(file);
        }

        [Test]
        public void GetFile_SpacesInPath_EncodedUri()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";
                var file = workspace.GetFileBuffer("file:///C:/My%20Project/query.sql", contents);
                Assert.NotNull(file);
                Assert.AreEqual(contents, file.Contents);
            });
        }

        [Test]
        public void GetFile_HashInPath_EncodedUri()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";
                var file = workspace.GetFileBuffer("file:///C:/C%23Project/query.sql", contents);
                Assert.NotNull(file);
                Assert.AreEqual(contents, file.Contents);

                // Should be retrievable with the same URI
                Assert.True(workspace.ContainsFile("file:///C:/C%23Project/query.sql"));
            });
        }

        [Test]
        public void GetFile_QuestionMarkInPath_EncodedUri()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";
                var file = workspace.GetFileBuffer("file:///C:/what%3F/query.sql", contents);
                Assert.NotNull(file);
                Assert.AreEqual(contents, file.Contents);

                Assert.True(workspace.ContainsFile("file:///C:/what%3F/query.sql"));
            });
        }

        [Test]
        public void GetFile_BracketsInPath()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";
                var file = workspace.GetFileBuffer("file:///C:/dev/[backup]/query.sql", contents);
                Assert.NotNull(file);
                Assert.AreEqual(contents, file.Contents);
            });
        }

        [Test]
        public void GetFile_DriveLetterPath_NoUriScheme()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                // GetScheme should return null for a Windows drive path
                Assert.Null(ServiceLayer.Workspace.Workspace.GetScheme(@"C:\Users\dev\query.sql"));
            });
        }

        [Test]
        public void GetFile_UncPath_NoUriScheme()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                Assert.Null(ServiceLayer.Workspace.Workspace.GetScheme(@"\\server\share\query.sql"));
            });
        }

        [Test]
        public void GetFile_CaseInsensitiveKey()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";

                // Open with uppercase path
                workspace.GetFileBuffer("file:///C:/Users/Dev/Query.sql", contents);

                // Should find it with lowercase key (LowercaseClientUri)
                Assert.True(workspace.ContainsFile("file:///c:/users/dev/query.sql"));
            });
        }

        [Test]
        public void GetFile_EscapedVsUnescapedUri_ResolveSameKey()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                string contents = "SELECT 1";

                // Open with encoded spaces
                workspace.GetFileBuffer("file:///C:/My%20Project/query.sql", contents);

                // Should resolve to the same key when using unencoded spaces
                Assert.True(workspace.ContainsFile("file:///C:/My Project/query.sql"));
            });
        }

        [Test]
        public void ContainsFile_ReturnsFalse_WhenFileNotOpened()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                Assert.False(workspace.ContainsFile("file:///C:/nonexistent/query.sql"));
            });
        }

        [Test]
        public void GetFile_BacktickEscapedBrackets_Unescaped()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                // Backtick-escaped brackets should be unescaped by UnescapePath
                string escaped = @"`[backup`]";
                string unescaped = ServiceLayer.Workspace.Workspace.UnescapePath(escaped);
                Assert.AreEqual("[backup]", unescaped);
            });
        }

        [Test]
        public void GetFile_BacktickEscapedSpaces_Unescaped()
        {
            // Backtick-escaped spaces should be unescaped
            string escaped = @"path`/to` file";
            string unescaped = ServiceLayer.Workspace.Workspace.UnescapePath(escaped);
            // Only backticks before [ ] and space are removed
            Assert.False(unescaped.Contains("`"));
        }

        [Test]
        public void GetFile_NoBackticks_PathUnchanged()
        {
            string path = @"C:\Users\dev\query.sql";
            string result = ServiceLayer.Workspace.Workspace.UnescapePath(path);
            Assert.AreEqual(path, result);
        }

        [Test]
        public async Task HandleDidOpenAndClose_FileUri_WindowsPath()
        {
            await RunIfWrapperAsync(async () =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                var workspaceService = new WorkspaceService<SqlToolsSettings> { Workspace = workspace };
                string uri = "file:///C:/Users/dev/query.sql";

                // Open
                var openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem { Uri = uri, Text = "SELECT 1" }
                };
                await workspaceService.HandleDidOpenTextDocumentNotification(openParams, eventContext: null);
                Assert.True(workspace.ContainsFile(uri));

                // Close
                var closeParams = new DidCloseTextDocumentParams
                {
                    TextDocument = new TextDocumentItem { Uri = uri }
                };
                await workspaceService.HandleDidCloseTextDocumentNotification(closeParams, eventContext: null);
                Assert.False(workspace.ContainsFile(uri));
            });
        }

        [Test]
        public async Task HandleDidOpen_GitScheme_IgnoredOnWindows()
        {
            var workspace = new ServiceLayer.Workspace.Workspace();
            var workspaceService = new WorkspaceService<SqlToolsSettings> { Workspace = workspace };

            var openParams = new DidOpenTextDocumentNotification
            {
                TextDocument = new TextDocumentItem { Uri = "git:/C:/repo/file.sql", Text = "SELECT 1" }
            };
            await workspaceService.HandleDidOpenTextDocumentNotification(openParams, eventContext: null);

            Assert.False(workspace.ContainsFile("git:/C:/repo/file.sql"));
        }

        [Test]
        [TestCase("file:///C:/path/file.sql")]
        [TestCase("file:///c%3A/path/file.sql")]
        [TestCase("file:///C:/path%20with%20spaces/file.sql")]
        [TestCase("file:///C:/C%23Project/file.sql")]
        [TestCase("file:///C:/what%3F/file.sql")]
        [TestCase("file:///C:/dev/[backup]/file.sql")]
        public void WorkspaceContainsFile_Windows(string uri)
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                var workspace = new ServiceLayer.Workspace.Workspace();
                workspace.GetFileBuffer(uri, "SELECT 1");
                Assert.True(workspace.ContainsFile(uri));
            });
        }

        [Test]
        public void GetScheme_WindowsDrivePath_ReturnsNull()
        {
            Assert.Null(ServiceLayer.Workspace.Workspace.GetScheme(@"C:\myfile.sql"));
            Assert.Null(ServiceLayer.Workspace.Workspace.GetScheme(@"D:\folder\myfile.sql"));
            Assert.Null(ServiceLayer.Workspace.Workspace.GetScheme(@"\\server\share\myfile.sql"));
        }

        [Test]
        public void GetScheme_FileUri_ReturnsFile()
        {
            Assert.AreEqual("file", ServiceLayer.Workspace.Workspace.GetScheme("file:///C:/path/file.sql"));
        }

        [Test]
        public void GetScheme_UntitledUri_ReturnsUntitled()
        {
            Assert.AreEqual("untitled", ServiceLayer.Workspace.Workspace.GetScheme("untitled:Untitled-1"));
        }

        [Test]
        public void GetScheme_GitUri_ReturnsGit()
        {
            Assert.AreEqual("git", ServiceLayer.Workspace.Workspace.GetScheme("git:/path/file.sql"));
        }

        /// <summary>
        /// Helper for async tests that should only run on Windows
        /// </summary>
        private static async Task RunIfWrapperAsync(Func<Task> test)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await test();
            }
        }

        #endregion

    }
}
