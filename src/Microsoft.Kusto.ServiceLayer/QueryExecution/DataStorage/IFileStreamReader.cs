//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a object that reads from the filesystem
    /// </summary>
    public interface IFileStreamReader : IDisposable
    {
        IList<DbCellValue> ReadRow(long offset, long rowId, IEnumerable<DbColumnWrapper> columns);
    }
}
