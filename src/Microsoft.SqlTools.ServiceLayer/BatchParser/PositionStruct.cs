//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    [Serializable]
    internal struct PositionStruct
    {
        private int _line;
        private int _column;
        private int _offset;
        private string _filename;

        public PositionStruct(int line, int column, int offset, string filename)
        {
            _line = line;
            _column = column;
            _offset = offset;
            _filename = filename;
        }

        public int Line { get { return _line; } }
        public int Column { get { return _column; } }
        public int Offset { get { return _offset; } }
        public string Filename { get { return _filename; } }
    }
}
