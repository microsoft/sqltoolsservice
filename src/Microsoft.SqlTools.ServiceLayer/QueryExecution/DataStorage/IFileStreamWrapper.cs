using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public interface IFileStreamWrapper : IDisposable
    {
        void Init(string fileName);
        void Init(string fileName, int bufferSize);
        Task<int> ReadData(byte[] buffer, int bytes);
        Task<int> ReadData(byte[] buffer, int bytes, long fileOffset);
        Task<int> WriteData(byte[] buffer, int bytes);
        Task<int> WriteData(byte[] buffer, int bytes, long fileOffset);
        Task Flush();
    }
}
