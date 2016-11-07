using System;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class SaveAsCsvFileStreamFactory : IFileStreamFactory
    {
        #region Properties

        public SaveResultsAsCsvRequestParams SaveRequestParams { get; set; }

        #endregion

        public string CreateFile()
        {
            throw new NotImplementedException();
        }

        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(new FileStreamWrapper(), fileName);
        }

        public IFileStreamWriter GetWriter(string fileName)
        {
            return new SaveAsCsvFileStreamWriter(new FileStreamWrapper(), fileName, SaveRequestParams);
        }

        public void DisposeFile(string fileName)
        {
            FileUtils.SafeFileDelete(fileName);
        }
    }
}
