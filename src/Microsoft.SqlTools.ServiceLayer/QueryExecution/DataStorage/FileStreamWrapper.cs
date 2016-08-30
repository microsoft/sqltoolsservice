using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class FileStreamWrapper : IFileStreamWrapper
    {
        #region Properties

        private const int DefaultBufferLength = 8192;

        private byte[] buffer;
        private int bufferDataSize;
        private FileStream fileStream;
        private long startOffset;
        private long currentOffset;

        #endregion

        public FileStreamWrapper()
        {
            // Initialize the internal state
            bufferDataSize = 0;
            startOffset = 0;
            currentOffset = 0;            
        }

        #region IFileStreamWrapper Implementation

        public void Init(string fileName)
        {
            Init(fileName, DefaultBufferLength);
        }

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
                fileInfo.Attributes |= System.IO.FileAttributes.Hidden;
            }
        }

        public async Task<int> ReadData(byte[] buf, int bytes)
        {
            return await ReadData(buf, bytes, currentOffset);
        }

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

        public async Task<int> WriteData(byte[] buf, int bytes, long offset)
        {
            // Make sure that we're initialized before performing operations
            if (buffer == null)
            {
                throw new InvalidOperationException("FileStreamWrapper must be initialized before performing operations");
            }

            long position = fileStream.Position;
            int retVal;
            try
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                await fileStream.WriteAsync(buf, 0, bytes);
                await fileStream.FlushAsync();

                retVal = bytes;
            }
            finally
            {
                fileStream.Position = position;
            }
            return retVal;
        }

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

        public static void DeleteFile(string fileName)
        {
            File.Delete(fileName);
        }

        #endregion

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
