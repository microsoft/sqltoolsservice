//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    [Serializable]
    internal struct PositionStruct
    {
        private int line;
        private int column;
        private int offset;
        private string filename;

        public PositionStruct(int line, int column, int offset, string filename)
        {
            this.line = line;
            this.column = column;
            this.offset = offset;
            this.filename = filename;
        }

        public int Line { get { return line; } }
        public int Column { get { return column; } }
        public int Offset { get { return offset; } }
        public string Filename { get { return filename; } }
    }
}
