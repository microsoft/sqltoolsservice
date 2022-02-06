using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public interface ISaveAsFileStreamWriter : IDisposable
    {
        void FlushBuffer();
        void WriteRow(IList<DbCellValue> row);
    }
}

