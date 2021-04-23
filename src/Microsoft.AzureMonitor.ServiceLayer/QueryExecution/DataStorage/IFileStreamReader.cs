using System;
using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;

namespace Microsoft.AzureMonitor.ServiceLayer.QueryExecution.DataStorage
{
    public interface IFileStreamReader: IDisposable
    {
        IList<DbCellValue> ReadRow(long offset, long rowId, IEnumerable<DbColumnWrapper> columns);
    }
}