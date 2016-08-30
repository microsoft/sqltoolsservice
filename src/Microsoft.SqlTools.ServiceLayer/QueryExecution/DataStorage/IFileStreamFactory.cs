
namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public interface IFileStreamFactory
    {

        IFileStreamReader GetReader(string fileName);

        IFileStreamWriter GetWriter(string fileName, int maxCharsToStore, int maxXmlCharsToStore);

        void DisposeFile(string fileName);

    }
}
