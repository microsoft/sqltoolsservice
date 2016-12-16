//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a object that reads from the filesystem
    /// </summary>
    public interface IFileStreamReader : IDisposable
    {
        IList<DbCellValue> ReadRow(long offset, IEnumerable<DbColumnWrapper> columns);
        FileStreamReadResult ReadInt16(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadInt32(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadInt64(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadByte(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadChar(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadBoolean(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadSingle(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadDouble(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadSqlDecimal(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadDecimal(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadDateTime(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadTimeSpan(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadString(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadBytes(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadDateTimeOffset(long i64Offset, DbColumnWrapper col);
        FileStreamReadResult ReadGuid(long offset, DbColumnWrapper col);
        FileStreamReadResult ReadMoney(long offset, DbColumnWrapper col);
    }
}
