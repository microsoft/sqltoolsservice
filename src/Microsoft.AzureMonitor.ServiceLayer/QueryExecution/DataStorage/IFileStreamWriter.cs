using System;
using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;

namespace Microsoft.AzureMonitor.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a object that writes to a filesystem wrapper
    /// </summary>
    public interface IFileStreamWriter : IDisposable
    {
        int WriteRow(StorageDataReader dataReader);
        void WriteRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns);
        void Seek(long offset);
        void FlushBuffer();
    }
}