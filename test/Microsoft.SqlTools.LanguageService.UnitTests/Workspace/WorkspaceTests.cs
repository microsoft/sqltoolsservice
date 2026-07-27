//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.LanguageService.UnitTests.Workspace
{
    public class WorkspaceTests
    {
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
        public void GetFileReturnsNullForPerforceFile()
        {
            // when I ask for a non-file object in the workspace, it should return null
            var workspace = new Microsoft.SqlTools.LanguageService.Workspace.Workspace();
            ScriptFile file = workspace.GetFile("perforce:myfile.sql");            
            Assert.Null(file);
        }

        [Test]
        public void DontBindToObjectExplorerConnectEvents()
        {
            // when I ask for a non-file object in the workspace, it should return null
            var workspace = new Microsoft.SqlTools.LanguageService.Workspace.Workspace();
            ScriptFile file = workspace.GetFile("objectexplorer://server;database=database;user=user");            
            Assert.Null(file);

            // when I ask for a file, it should return the file
            string tempFile = Path.GetTempFileName();
            string fileContents = "hello world";
            File.WriteAllText(tempFile, fileContents);

            file = workspace.GetFile(tempFile);
            Assert.AreEqual(fileContents, file.Contents);

            var fileUri = new Uri(tempFile).AbsoluteUri;
            file = workspace.GetFile(fileUri);
            Assert.AreEqual(fileContents, file.Contents);

            file = workspace.GetFileBuffer("untitled://"+ tempFile, fileContents);
            Assert.AreEqual(fileContents, file.Contents);

            // For windows files, just check scheme is null since it's hard to mock file contents in these
            Assert.Null(Microsoft.SqlTools.LanguageService.Workspace.Workspace.GetScheme(@"C:\myfile.sql"));
            Assert.Null(Microsoft.SqlTools.LanguageService.Workspace.Workspace.GetScheme(@"\\myfile.sql"));
        }
    }
}
