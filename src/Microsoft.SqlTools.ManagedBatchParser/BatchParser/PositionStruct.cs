﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    [Serializable]
    public struct PositionStruct
    {
        private readonly int line;
        private readonly int column;
        private readonly int offset;
        private readonly string filename;

        /// <summary>
        /// Constructor for the PositionStruct class
        /// </summary>
        public PositionStruct(int line, int column, int offset, string filename)
        {
            this.line = line;
            this.column = column;
            this.offset = offset;
            this.filename = filename;
        }

        /// <summary>
        /// Get line from the PositionStruct
        /// </summary>
        public int Line { get { return line; } }

        /// <summary>
        /// Get column from the PositionStruct
        /// </summary>
        public int Column { get { return column; } }

        /// <summary>
        /// Get offset from the PositionStruct
        /// </summary>
        public int Offset { get { return offset; } }

        /// <summary>
        /// Get file name from the PositionStruct
        /// </summary>
        public string Filename { get { return filename; } }
    }
}
