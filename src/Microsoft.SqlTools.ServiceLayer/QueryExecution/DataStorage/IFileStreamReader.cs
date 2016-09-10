//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a object that reads from the filesystem
    /// </summary>
    public interface IFileStreamReader : IDisposable
    {
        object[] ReadRow(long offset, IEnumerable<DbColumnWrapper> columns);
        FileStreamReadResult<short>  ReadInt16(long i64Offset);
        FileStreamReadResult<int>  ReadInt32(long i64Offset);
        FileStreamReadResult<long> ReadInt64(long i64Offset);
        FileStreamReadResult<byte> ReadByte(long i64Offset);
        FileStreamReadResult<char>  ReadChar(long i64Offset);
        FileStreamReadResult<bool>  ReadBoolean(long i64Offset);
        FileStreamReadResult<float>  ReadSingle(long i64Offset);
        FileStreamReadResult<double>  ReadDouble(long i64Offset);
        FileStreamReadResult<SqlDecimal>  ReadSqlDecimal(long i64Offset);
        FileStreamReadResult<decimal>  ReadDecimal(long i64Offset);
        FileStreamReadResult<DateTime>  ReadDateTime(long i64Offset);
        FileStreamReadResult<TimeSpan>  ReadTimeSpan(long i64Offset);
        FileStreamReadResult<string>  ReadString(long i64Offset);
        FileStreamReadResult<byte[]>  ReadBytes(long i64Offset);
        FileStreamReadResult<DateTimeOffset>  ReadDateTimeOffset(long i64Offset);
    }
}
