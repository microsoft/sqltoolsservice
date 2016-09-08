//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.DataStorage
{
    public class FileStreamWrapperTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public void InitInvalidFilenameParameter(string fileName)
        {
            // If:
            // ... I have a file stream wrapper that is initialized with invalid fileName
            // Then:
            // ... It should throw an argument null exception
            using (FileStreamWrapper fsw = new FileStreamWrapper())
            {
                Assert.Throws<ArgumentNullException>(() => fsw.Init(fileName, 8192, FileAccess.Read));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void InitInvalidBufferLength(int bufferLength)
        {
            // If:
            // ... I have a file stream wrapper that is initialized with an invalid buffer length
            // Then:
            // ... I should throw an argument out of range exception
            using (FileStreamWrapper fsw = new FileStreamWrapper())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => fsw.Init("validFileName", bufferLength, FileAccess.Read));
            }
        }

        [Fact]
        public void InitInvalidFileAccessMode()
        {
            // If:
            // ... I attempt to open a file stream wrapper that is initialized with an invalid file
            //     access mode
            // Then:
            // ... I should get an invalid argument exception
            using (FileStreamWrapper fsw = new FileStreamWrapper())
            {
                Assert.Throws<ArgumentException>(() => fsw.Init("validFileName", 8192, FileAccess.Write));
            }
        }

        [Fact]
        public void InitSuccessful()
        {
            string fileName = Path.GetTempFileName();

            try
            {
                using (FileStreamWrapper fsw = new FileStreamWrapper())
                {
                    // If:
                    // ... I have a file stream wrapper that is initialized with valid parameters
                    fsw.Init(fileName, 8192, false);

                    // Then:
                    // ... The file should exist
                    FileInfo fileInfo = new FileInfo(fileName);
                    Assert.True(fileInfo.Exists);

                    // ... The file should be marked as hidden
                    Assert.True((fileInfo.Attributes & FileAttributes.Hidden) != 0);
                }
            }
            finally
            {
                // Cleanup:
                // ... Delete the file that was created
                try { File.Delete(fileName); } catch { /* Don't care */ }
            }
        }

        [Fact]
        public void PerformOpWithoutInit()
        {
            byte[] buf = new byte[10];

            using (FileStreamWrapper fsw = new FileStreamWrapper())
            {
                // If:
                // ... I have a file stream wrapper that hasn't been initialized
                // Then:
                // ... Attempting to perform any operation will result in an exception
                Assert.Throws<InvalidOperationException>(() => fsw.ReadData(buf, 1));
                Assert.Throws<InvalidOperationException>(() => fsw.ReadData(buf, 1, 0));
                Assert.Throws<InvalidOperationException>(() => fsw.WriteData(buf, 1));
                Assert.Throws<InvalidOperationException>(() => fsw.Flush());
            }
        }

        [Fact]
        public void PerformWriteOpOnReadOnlyWrapper()
        {
            byte[] buf = new byte[10];

            using (FileStreamWrapper fsw = new FileStreamWrapper())
            {
                // If:
                // ... I have a readonly file stream wrapper
                // Then:
                // ... Attempting to perform any write operation should result in an exception
                Assert.Throws<InvalidOperationException>(() => fsw.WriteData(buf, 1));
                Assert.Throws<InvalidOperationException>(() => fsw.Flush());
            }
        }

        [Theory]
        [InlineData(1024, 20, 10)]   // Standard scenario
        [InlineData(1024, 100, 100)] // Requested more bytes than there are
        [InlineData(5, 20, 10)]     // Internal buffer too small, force a move-to operation   
        public void ReadData(int internalBufferLength, int outBufferLength, int requestedBytes)
        {
            // Setup:
            // ... I have a file that has a handful of bytes in it
            string fileName = Path.GetTempFileName();
            const string stringToWrite = "hello";
            CreateTestFile(fileName, stringToWrite);
            byte[] targetBytes = Encoding.Unicode.GetBytes(stringToWrite);

            try
            {
                // If:
                // ... I have a file stream wrapper that has been initialized to an existing file
                // ... And I read some bytes from it
                int bytesRead;
                byte[] buf = new byte[outBufferLength];
                using (FileStreamWrapper fsw = new FileStreamWrapper())
                {
                    fsw.Init(fileName, internalBufferLength, true);
                    bytesRead = fsw.ReadData(buf, targetBytes.Length);
                }

                // Then:
                // ... I should get those bytes back
                Assert.Equal(targetBytes.Length, bytesRead);
                Assert.True(targetBytes.Take(targetBytes.Length).SequenceEqual(buf.Take(targetBytes.Length)));
                
            }
            finally
            {
                // Cleanup:
                // ... Delete the test file
                CleanupTestFile(fileName);
            }
        }

        [Theory]
        [InlineData(1024)]  // Standard scenario
        [InlineData(10)]    // Internal buffer too small, forces a flush
        public void WriteData(int internalBufferLength)
        {
            string fileName = Path.GetTempFileName();
            byte[] bytesToWrite = Encoding.Unicode.GetBytes("hello");

            try
            {
                // If:
                // ... I have a file stream that has been initialized
                // ... And I write some bytes to it
                using (FileStreamWrapper fsw = new FileStreamWrapper())
                {
                    fsw.Init(fileName, internalBufferLength, false);
                    int bytesWritten = fsw.WriteData(bytesToWrite, bytesToWrite.Length);

                    Assert.Equal(bytesToWrite.Length, bytesWritten);
                }

                // Then:
                // ... The file I wrote to should contain only the bytes I wrote out
                using (FileStream fs = File.OpenRead(fileName))
                {
                    byte[] readBackBytes = new byte[1024];
                    int bytesRead = fs.Read(readBackBytes, 0, readBackBytes.Length);

                    Assert.Equal(bytesToWrite.Length, bytesRead);   // If bytes read is not equal, then more or less of the original string was written to the file
                    Assert.True(bytesToWrite.SequenceEqual(readBackBytes.Take(bytesRead)));
                }
            }
            finally
            {
                // Cleanup:
                // ... Delete the test file
                CleanupTestFile(fileName);
            }
        }

        private static void CreateTestFile(string fileName, string value)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                byte[] bytesToWrite = Encoding.Unicode.GetBytes(value);
                fs.Write(bytesToWrite, 0, bytesToWrite.Length);
                fs.Flush();
            }
        }

        private static void CleanupTestFile(string fileName)
        {
            try { File.Delete(fileName); } catch { /* Don't Care */}
        }
    }
}
