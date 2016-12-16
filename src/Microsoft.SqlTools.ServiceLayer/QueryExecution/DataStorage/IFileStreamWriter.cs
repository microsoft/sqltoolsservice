//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlTypes;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a object that writes to a filesystem wrapper
    /// </summary>
    public interface IFileStreamWriter : IDisposable
    {
        int WriteRow(StorageDataReader dataReader);
        int WriteNull();
        int WriteInt16(short val);
        int WriteInt32(int val);
        int WriteInt64(long val);
        int WriteByte(byte val);
        int WriteChar(char val);
        int WriteBoolean(bool val);
        int WriteSingle(float val);
        int WriteDouble(double val);
        int WriteDecimal(decimal val);
        int WriteSqlDecimal(SqlDecimal val);
        int WriteDateTime(DateTime val);
        int WriteDateTimeOffset(DateTimeOffset dtoVal);
        int WriteTimeSpan(TimeSpan val);
        int WriteString(string val);
        int WriteBytes(byte[] bytes);
        int WriteGuid(Guid val);
        int WriteMoney(SqlMoney val);
        void FlushBuffer();
    }
}
