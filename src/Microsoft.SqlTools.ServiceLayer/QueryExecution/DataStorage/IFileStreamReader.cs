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
    }
}
