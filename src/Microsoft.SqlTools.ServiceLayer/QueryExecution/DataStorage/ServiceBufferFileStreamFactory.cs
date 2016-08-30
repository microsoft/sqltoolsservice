namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class ServiceBufferFileStreamFactory : IFileStreamFactory
    {
        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(new FileStreamWrapper(), fileName);
        }

        public IFileStreamWriter GetWriter(string fileName, int maxCharsToStore, int maxXmlCharsToStore)
        {
            return new ServiceBufferFileStreamWriter(new FileStreamWrapper(), fileName, maxCharsToStore, maxXmlCharsToStore);
        }

        public void DisposeFile(string fileName)
        {
            try
            {
                FileStreamWrapper.DeleteFile(fileName);
            }
            catch
            {
                // If we have problems deleting the file from a temp location, we don't really care
            }
        }
    }
}
