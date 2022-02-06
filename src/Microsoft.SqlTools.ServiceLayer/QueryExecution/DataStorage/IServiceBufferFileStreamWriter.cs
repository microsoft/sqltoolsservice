using System;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public interface IServiceBufferFileStreamWriter : IDisposable
    {
        int WriteRow(StorageDataReader dataReader);
        void Seek(long offset);
        void FlushBuffer();
    }
}
