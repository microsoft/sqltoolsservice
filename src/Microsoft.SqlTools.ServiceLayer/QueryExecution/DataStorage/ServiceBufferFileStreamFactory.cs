namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class ServiceBufferFileStreamFactory : IFileStreamFactory
    {
        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(fileName);
        }

        public IFileStreamWriter GetWriter(string fileName, int maxCharsToStore, int maxXmlCharsToStore)
        {
            return new ServiceBufferFileStreamWriter(fileName, maxCharsToStore, maxXmlCharsToStore);
        }

    }
}
