//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Wrapper for a file stream, providing simplified creation, deletion, read, and write
    /// functionality.
    /// </summary>
    public class FileStreamWrapper : IFileStreamWrapper
    {
        #region Member Variables

        private byte[] buffer;
        private int bufferDataSize;
        private FileStream fileStream;
        private long startOffset;
        private long currentOffset;

        #endregion

        /// <summary>
        /// Constructs a new FileStreamWrapper and initializes its state.
        /// </summary>
        public FileStreamWrapper()
        {
            // Initialize the internal state
            bufferDataSize = 0;
            startOffset = 0;
            currentOffset = 0;            
        }

        #region IFileStreamWrapper Implementation

        /// <summary>
        /// Initializes the wrapper by creating the internal buffer and opening the requested file.
        /// If the file does not already exist, it will be created.
        /// </summary>
        /// <param name="fileName">Name of the file to open/create</param>
        /// <param name="bufferLength">The length of the internal buffer</param>
        public void Init(string fileName, int bufferLength)
        {
            // Sanity check for valid buffer length
            if (bufferLength <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            // Setup the buffer
            buffer = new byte[bufferLength];

            // Open the requested file for reading/writing, creating one if it doesn't exist
            fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite,
                bufferLength, false /*don't use asyncio*/);

            // make file hidden
            FileInfo fileInfo = new FileInfo(fileName);
            if (fileInfo.Exists)
            {
                fileInfo.Attributes |= FileAttributes.Hidden;
            }
        }

        /// <summary>
        /// Reads data into a buffer from the current offset into the file
        /// </summary>
        /// <param name="buf">The buffer to output the read data to</param>
        /// <param name="bytes">The number of bytes to read into the buffer</param>
        /// <returns>The number of bytes read</returns>
        public Task<int> ReadData(byte[] buf, int bytes)
        {
            return ReadData(buf, bytes, currentOffset);
        }

        /// <summary>
        /// Reads data into a buffer from the specified offset into the file
        /// </summary>
        /// <param name="buf">The buffer to output the read data to</param>
        /// <param name="bytes">The number of bytes to read into the buffer</param>
        /// <param name="offset">The offset into the file to start reading bytes from</param>
        /// <returns>The number of bytes read</returns>
        public async Task<int> ReadData(byte[] buf, int bytes, long offset)
        {
            // Make sure that we're initialized before performing operations
            if (buffer == null)
            {
                throw new InvalidOperationException("FileStreamWrapper must be initialized before performing operations");
            }

            await MoveTo(offset);

            int bytesCopied = 0;
            while (bytesCopied < bytes)
            {
                int bufferOffset = (int)(currentOffset - startOffset);
                int bytesToCopy = (bytes - bytesCopied);
                if (bytesToCopy > (bufferDataSize - bufferOffset))
                {
                    bytesToCopy = bufferDataSize - bufferOffset;
                }
                Buffer.BlockCopy(buffer, bufferOffset, buf, bytesCopied, bytesToCopy);
                bytesCopied += bytesToCopy;

                if (bytesCopied < bytes &&             // did not get all the bytes yet
                    bufferDataSize == buffer.Length)   // since current data buffer is full we should continue reading the file
                {
                    // move forward one full length of the buffer
                    await MoveTo(startOffset + buffer.Length);
                }
                else
                {
                    // copied all the bytes simply adjust the current buffer pointer
                    currentOffset += bytesToCopy;
                }
            }
            return bytesCopied;
        }

        /// <summary>
        /// Writes data to the underlying filestream, with buffering.
        /// </summary>
        /// <param name="buf">The buffer of bytes to write to the filestream</param>
        /// <param name="bytes">The number of bytes to write</param>
        /// <returns>The number of bytes written</returns>
        public async Task<int> WriteData(byte[] buf, int bytes)
        {
            // Make sure that we're initialized before performing operations
            if (buffer == null)
            {
                throw new InvalidOperationException("FileStreamWrapper must be initialized before performing operations");
            }

            int bytesCopied = 0;
            while (bytesCopied < bytes)
            {
                int bufferOffset = (int)(currentOffset - startOffset);
                int bytesToCopy = (bytes - bytesCopied);
                if (bytesToCopy > (buffer.Length - bufferOffset))
                {
                    bytesToCopy = buffer.Length - bufferOffset;
                }
                Buffer.BlockCopy(buf, bytesCopied, buffer, bufferOffset, bytesToCopy);
                bytesCopied += bytesToCopy;

                // adjust the current buffer pointer
                currentOffset += bytesToCopy;

                if (bytesCopied < bytes) // did not get all the bytes yet
                {
                    Debug.Assert((int)(currentOffset - startOffset) == buffer.Length);
                    // flush buffer
                    await Flush();
                }
            }
            Debug.Assert(bytesCopied == bytes);
            return bytesCopied;
        }

        /// <summary>
        /// Flushes the internal buffer to the filestream
        /// </summary>
        public async Task Flush()
        {
            // Make sure that we're initialized before performing operations
            if (buffer == null)
            {
                throw new InvalidOperationException("FileStreamWrapper must be initialized before performing operations");
            }

            // Make sure we are at the right place in the file
            Debug.Assert(fileStream.Position == startOffset);

            int bytesToWrite = (int)(currentOffset - startOffset);
            await fileStream.WriteAsync(buffer, 0, bytesToWrite);
            startOffset += bytesToWrite;
            await fileStream.FlushAsync();

            Debug.Assert(startOffset == currentOffset);
        }

        /// <summary>
        /// Deletes the given file (ideally, created with this wrapper) from the filesystem
        /// </summary>
        /// <param name="fileName">The path to the file to delete</param>
        public static void DeleteFile(string fileName)
        {
            File.Delete(fileName);
        }

        #endregion

        /// <summary>
        /// Moves the internal buffer to the specified offset into the file
        /// </summary>
        /// <param name="offset">Offset into the file to move to</param>
        private async Task MoveTo(long offset)
        {
            if (buffer.Length > bufferDataSize ||         // buffer is not completely filled
                offset < startOffset ||                   // before current buffer start
                offset >= (startOffset + buffer.Length))  // beyond current buffer end
            {
                // init the offset
                startOffset = offset;

                // position file pointer
                fileStream.Seek(startOffset, SeekOrigin.Begin);

                // fill in the buffer
                bufferDataSize = await fileStream.ReadAsync(buffer, 0, buffer.Length);
            }
            // make sure to record where we are
            currentOffset = offset;
        }

        #region IDisposable Implementation

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                fileStream.Dispose();
            }

            disposed = true;
        }

        ~FileStreamWrapper()
        {
            Dispose(false);
        }

        #endregion
    }
}
