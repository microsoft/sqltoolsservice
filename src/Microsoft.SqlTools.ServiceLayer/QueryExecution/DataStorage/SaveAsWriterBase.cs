using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public abstract class SaveAsStreamWriter : IFileStreamWriter
    {
        

        protected SaveAsStreamWriter(Stream stream, SaveResultsRequestParams requestParams)
        {
            FileStream = stream;
            var saveParams = requestParams;
            if (requestParams.IsSaveSelection)
            {
                // ReSharper disable PossibleInvalidOperationException  IsSaveSelection verifies these values exist
                ColumnStartIndex = saveParams.ColumnStartIndex.Value;
                ColumnCount = saveParams.ColumnEndIndex.Value - saveParams.ColumnStartIndex.Value;
                // ReSharper restore PossibleInvalidOperationException
            }
        }

        #region Properties

        protected int? ColumnStartIndex { get; private set; }

        protected int? ColumnCount { get; private set; }

        protected Stream FileStream { get; private set; }

        #endregion

        [Obsolete]
        public int WriteRow(StorageDataReader dataReader)
        {
            throw new InvalidOperationException("This type of writer is meant to write values from a list of cell values only.");
        }

        public abstract void WriteRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns);

        public void FlushBuffer()
        {
            FileStream.Flush();
        }

        #region IDisposable Implementation

        private bool disposed;

        private void Dispose(bool disposing)
        {
            if (disposed || !disposing)
            {
                disposed = true;
                return;
            }

            FileStream.Flush();
            FileStream.Dispose();
        }
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
