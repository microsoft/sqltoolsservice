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
    /// Interface for a file stream writer that is used for saving results.
    /// </summary>
    public interface ISaveAsFileStreamWriter : IDisposable
    {
        /// <summary>
        /// Flushes the buffer to the output file.
        /// </summary>
        void FlushBuffer();

        /// <summary>
        /// Writes a row to the output file.
        /// </summary>
        /// <param name="row">Contents of the row to write.</param>
        void WriteRow(IList<DbCellValue> row);
    }
}

