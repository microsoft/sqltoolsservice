//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a wrapper around a filesystem reader/writer, mainly for unit testing purposes
    /// </summary>
    public interface IFileStreamWrapper : IDisposable
    {
        void Init(string fileName, int bufferSize);
        Task<int> ReadData(byte[] buffer, int bytes);
        Task<int> ReadData(byte[] buffer, int bytes, long fileOffset);
        Task<int> WriteData(byte[] buffer, int bytes);
        Task Flush();
    }
}
