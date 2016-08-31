//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Represents a value returned from a read from a file stream. This is used to eliminate ref
    /// parameters used in the read methods of the SSMS code this was based on.
    /// </summary>
    /// <typeparam name="T">The type of the value that was read</typeparam>
    public struct FileStreamReadResult<T>
    {
        /// <summary>
        /// Whether or not the value of the field is null
        /// </summary>
        public bool IsNull { get; set; }

        /// <summary>
        /// The value of the field. If <see cref="IsNull"/> is true, this will be set to <c>default(T)</c>
        /// </summary>
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

        /// <summary>
        /// Constructs a new FileStreamReadResult
        /// </summary>
        /// <param name="value">The value of the result</param>
        /// <param name="totalLength">The number of bytes for the used to store the value's length and value</param>
        /// <param name="isNull">Whether or not the value is <c>null</c></param>
        public FileStreamReadResult(T value, int totalLength, bool isNull)
        {
            Value = value;
            TotalLength = totalLength;
            IsNull = isNull;
        }
    }
}
