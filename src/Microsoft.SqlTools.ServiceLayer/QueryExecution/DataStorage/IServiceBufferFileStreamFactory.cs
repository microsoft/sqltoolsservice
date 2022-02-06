namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public interface IServiceBufferFileStreamFactory
    {
        string CreateFile();

        IFileStreamReader GetReader(string fileName);

        IServiceBufferFileStreamWriter GetWriter(string fileName);

        void DisposeFile(string fileName);
    }
}
