namespace Microsoft.AzureMonitor.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a factory that creates filesystem readers/writers
    /// </summary>
    public interface IFileStreamFactory
    {
        string CreateFile();

        IFileStreamReader GetReader(string fileName);

        IFileStreamWriter GetWriter(string fileName);

        void DisposeFile(string fileName);

    }
}