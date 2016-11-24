using System;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class SaveAsJsonFileStreamFactory : IFileStreamFactory
    {

        #region Properties

        public SaveResultsAsJsonRequestParams SaveRequestParams { get; set; }

        #endregion

        public string CreateFile()
        {
            throw new NotImplementedException();
        }

        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
        }

        public IFileStreamWriter GetWriter(string fileName)
        {
            return new SaveAsJsonFileStreamWriter(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite), SaveRequestParams);
        }

        public void DisposeFile(string fileName)
        {
            FileUtils.SafeFileDelete(fileName);
        }

    }
}
