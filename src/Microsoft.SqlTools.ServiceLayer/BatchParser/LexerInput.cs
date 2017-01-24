//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    internal sealed class LexerInput : IDisposable
    {
        private readonly string _filename;
        private TextReader _input;
        private int _currentLine;
        private int _currentColumn;
        private int _bufferStartOffset;
        private int _currentSbOffset;
        private StringBuilder _buffer;

        public LexerInput(TextReader reader, string filename)
        {
            _input = reader;
            _filename = filename;
            _currentLine = 1;
            _currentColumn = 1;
            _bufferStartOffset = 0;
            _currentSbOffset = 0;
            _buffer = new StringBuilder();
            EnsureBytes(1);
        }

        public string Filename
        {
            get { return _filename; }
        }

        public int CurrentLine 
        {
            get { return _currentLine; }
        }

        public int CurrentColumn 
        {
            get { return _currentColumn; }
        }

        public void Consume()
        {
            bool newLineWithCR = false;

            char? ch = Lookahead();
            if (ch == null)
            {
                // end of stream
                return;
            }
            else if (ch == '\r')
            {
                newLineWithCR = true;
            }
            else if (ch == '\n')
            {
                _currentLine++;
                _currentColumn = 0;
            }

            int count = EnsureBytes(1);
            if (count == 0)
            {
                // end of stream
                return;
            }
            _currentSbOffset++;

            if (newLineWithCR && Lookahead() != '\n')
            {
                _currentLine++;
                _currentColumn = 0;
            }
            _currentColumn++;
        }

        public void Dispose()
        {
            if (_input != null)
            {
                _input.Dispose();
                _input = null;
            }
        }

        public int CurrentOffset
        {
            get { return _bufferStartOffset + _currentSbOffset; }
        }

        public int EnsureBytes(int bytesToBuffer)
        {
            if (_currentSbOffset + bytesToBuffer > _buffer.Length)
            {
                if (_input == null)
                {
                    return _buffer.Length - _currentSbOffset;
                }
                int chArrayLength = bytesToBuffer - (_buffer.Length - _currentSbOffset) + 128;
                char[] chArray = new char[chArrayLength];
                int count = _input.ReadBlock(chArray, 0, chArrayLength);
                _buffer.Append(chArray, 0, count);
                if (count < chArrayLength)
                {
                    _input.Dispose();
                    _input = null;
                }
                return _buffer.Length - _currentSbOffset;
            }
            return bytesToBuffer;
        }

        public char? Lookahead()
        {
            int count = EnsureBytes(1);
            if (count == 0)
            {
                return null;
            }
            return _buffer[_currentSbOffset];
        }

        public char? Lookahead(int lookahead)
        {
            int count = EnsureBytes(lookahead + 1);
            if (count < lookahead + 1)
            {
                return null;
            }
            return _buffer[_currentSbOffset + lookahead];
        }

        public string FlushBufferedText()
        {
            string text;

            text = _buffer.ToString(0, _currentSbOffset);
            _bufferStartOffset += _currentSbOffset;
            _buffer.Remove(0, _currentSbOffset);
            _currentSbOffset = 0;

            return text;
        }
    }
}
