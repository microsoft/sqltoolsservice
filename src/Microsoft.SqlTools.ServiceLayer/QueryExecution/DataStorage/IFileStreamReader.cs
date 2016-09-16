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
        DbCellValue[] ReadRow(long offset, IEnumerable<DbColumnWrapper> columns);
        FileStreamReadResult ReadInt16(long i64Offset);
        FileStreamReadResult ReadInt32(long i64Offset);
        FileStreamReadResult ReadInt64(long i64Offset);
        FileStreamReadResult ReadByte(long i64Offset);
        FileStreamReadResult ReadChar(long i64Offset);
        FileStreamReadResult ReadBoolean(long i64Offset);
        FileStreamReadResult ReadSingle(long i64Offset);
        FileStreamReadResult ReadDouble(long i64Offset);
        FileStreamReadResult ReadSqlDecimal(long i64Offset);
        FileStreamReadResult ReadDecimal(long i64Offset);
        FileStreamReadResult ReadDateTime(long i64Offset);
        FileStreamReadResult ReadTimeSpan(long i64Offset);
        FileStreamReadResult ReadString(long i64Offset);
        FileStreamReadResult ReadBytes(long i64Offset);
        FileStreamReadResult ReadDateTimeOffset(long i64Offset);
    }
}
