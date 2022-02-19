//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a writer that stores data to a service buffer file.
    /// </summary>
    public interface IServiceBufferFileStreamWriter : IDisposable
    {
        /// <summary>
        /// Write a row to the service buffer file.
        /// </summary>
        /// <param name="dataReader">Database reader that's queued up with results to read.</param>
        /// <returns>Number of bytes written to the file.</returns>
        int WriteRow(StorageDataReader dataReader);

        /// <summary>
        /// Seek to a position in the service buffer file.
        /// </summary>
        /// <param name="offset">Offset from the beginning of the file to seek to.</param>
        void Seek(long offset);

        /// <summary>
        /// Flushes the internal buffer to the file stream.
        /// </summary>
        void FlushBuffer();
    }
}
