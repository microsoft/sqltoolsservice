//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    /// <summary>
    /// Lexer input class used by lexer in SMO Batch Parser
    /// </summary>
    internal sealed class LexerInput : IDisposable
    {
        private readonly string filename;
        private TextReader input;
        private int currentLine;
        private int currentColumn;
        private int bufferStartOffset;
        private int currentSbOffset;
        private StringBuilder buffer;

        /// <summary>
        /// Constructor method for the LexerInput class
        /// </summary>
        public LexerInput(TextReader reader, string filename)
        {
            input = reader;
            this.filename = filename;
            currentLine = 1;
            currentColumn = 1;
            bufferStartOffset = 0;
            currentSbOffset = 0;
            buffer = new StringBuilder();
            EnsureBytes(1);
        }

        /// <summary>
        /// Get filename associated with lexer input
        /// </summary>
        public string Filename
        {
            get { return filename; }
        }

        /// <summary>
        /// Get current line associated with lexer input
        /// </summary>
        public int CurrentLine 
        {
            get { return currentLine; }
        }

        /// <summary>
        /// Get current column associated with lexer input
        /// </summary>
        public int CurrentColumn 
        {
            get { return currentColumn; }
        }

        /// <summary>
        /// Consume token used by lexer input
        /// </summary>
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
                currentLine++;
                currentColumn = 0;
            }

            int count = EnsureBytes(1);
            if (count == 0)
            {
                // end of stream
                return;
            }
            currentSbOffset++;

            if (newLineWithCR && Lookahead() != '\n')
            {
                currentLine++;
                currentColumn = 0;
            }
            currentColumn++;
        }

        public void Dispose()
        {
            if (input != null)
            {
                input.Dispose();
                input = null;
            }
        }

        /// <summary>
        /// Get current offset for the lexer input
        /// </summary>
        public int CurrentOffset
        {
            get { return bufferStartOffset + currentSbOffset; }
        }

        /// <summary>
        /// Ensure correct number of bytes to buffer
        /// </summary>
        public int EnsureBytes(int bytesToBuffer)
        {
            if (currentSbOffset + bytesToBuffer > buffer.Length)
            {
                if (input == null)
                {
                    return buffer.Length - currentSbOffset;
                }
                int chArrayLength = bytesToBuffer - (buffer.Length - currentSbOffset) + 128;
                char[] chArray = new char[chArrayLength];
                int count = input.ReadBlock(chArray, 0, chArrayLength);
                buffer.Append(chArray, 0, count);
                if (count < chArrayLength)
                {
                    input.Dispose();
                    input = null;
                }
                return buffer.Length - currentSbOffset;
            }
            return bytesToBuffer;
        }

        /// <summary>
        /// look ahead bytes in lexer input
        /// </summary>
        public char? Lookahead()
        {
            int count = EnsureBytes(1);
            if (count == 0)
            {
                return null;
            }
            return buffer[currentSbOffset];
        }

        /// <summary>
        /// look ahead bytes in lexer input
        /// </summary>
        public char? Lookahead(int lookahead)
        {
            int count = EnsureBytes(lookahead + 1);
            if (count < lookahead + 1)
            {
                return null;
            }
            return buffer[currentSbOffset + lookahead];
        }

        /// <summary>
        /// Flush buffered text in lexer input
        /// </summary>
        public string FlushBufferedText()
        {
            string text;

            text = buffer.ToString(0, currentSbOffset);
            bufferStartOffset += currentSbOffset;
            buffer.Remove(0, currentSbOffset);
            currentSbOffset = 0;

            return text;
        }
    }
}
