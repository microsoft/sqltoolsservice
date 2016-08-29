using System;
using System.Data.SqlTypes;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public interface IFileStreamWriter : IDisposable
    {
        Task<int> WriteNull();
        Task<int> WriteInt16(short val);
        Task<int> WriteInt32(int val);
        Task<int> WriteInt32(long offset, int val);
        Task<int> WriteInt64(long val);
        Task<int> WriteByte(byte val);
        Task<int> WriteChar(char val);
        Task<int> WriteBoolean(bool val);
        Task<int> WriteSingle(float val);
        Task<int> WriteDouble(double val);
        Task<int> WriteDecimal(decimal val);
        Task<int> WriteSqlDecimal(SqlDecimal val);
        Task<int> WriteDateTime(DateTime val);
        Task<int> WriteDateTimeOffset(DateTimeOffset dtoVal);
        Task<int> WriteTimeSpan(TimeSpan val);
        Task<int> WriteString(string val);
        Task<int> WriteBytes(byte[] bytes, int length);
        Task FlushBuffer();
    }
}
