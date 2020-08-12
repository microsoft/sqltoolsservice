//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Represents a value returned from a read from a file stream. This is used to eliminate ref
    /// parameters used in the read methods.
    /// </summary>
    public struct FileStreamReadResult
    {
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

        /// <summary>
        /// Value of the cell
        /// </summary>
        public DbCellValue Value { get; set; }

        /// <summary>
        /// Constructs a new FileStreamReadResult
        /// </summary>
        /// <param name="value">The value of the result, ready for consumption by a client</param>
        /// <param name="totalLength">The number of bytes for the used to store the value's length and value</param>s
        public FileStreamReadResult(DbCellValue value, int totalLength)
        {
            Value = value;
            TotalLength = totalLength;
        }
    }
}
