
namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public interface IFileStreamFactory
    {

        IFileStreamReader GetReader(string fileName);

        IFileStreamWriter GetWriter(string fileName);

    }
}
