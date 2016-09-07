//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a wrapper around a filesystem reader/writer, mainly for unit testing purposes
    /// </summary>
    public interface IFileStreamWrapper : IDisposable
    {
        void Init(string fileName, int bufferSize, bool forReadingOnly);
        int ReadData(byte[] buffer, int bytes);
        int ReadData(byte[] buffer, int bytes, long fileOffset);
        int WriteData(byte[] buffer, int bytes);
        void Flush();
    }
}
