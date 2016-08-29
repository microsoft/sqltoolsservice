using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public struct FileStreamReadResult<T>
    {
        public bool IsNull { get; set; }

        public T Value { get; set; }

        /// <summary>
        /// The total length in bytes of the value, (including the bytes used to store the length
        /// of the value)
        /// </summary>
        /// <remarks>
        /// Cell values are stored such that the length of the value is stored first, then the
        /// value itself is stored. Eg, a string may be stored as 0x03 0x6C 0x6F 0x6C. Under this
        /// system, the value would be "lol", the length would be 3, and the total length would be
        /// 4 bytes.
        /// </remarks>
        public int TotalLength { get; set; }

        public FileStreamReadResult(T value, int totalLength, bool isNull)
        {
            Value = value;
            TotalLength = totalLength;
            IsNull = isNull;
        }
    }
}
