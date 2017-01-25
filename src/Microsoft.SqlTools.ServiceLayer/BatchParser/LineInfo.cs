//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    class LineInfo
    {
        private IEnumerable<Token> tokens;
        private IEnumerable<VariableReference> variableRefs;

        public LineInfo(IEnumerable<Token> tokens, IEnumerable<VariableReference> variableRefs)
        {
            this.tokens = tokens;
            this.variableRefs = variableRefs;
        }

        public PositionStruct GetStreamPositionForOffset(int offset)
        {
            if (variableRefs != null)
            {
                offset = CalculateVarsUnresolvedOffset(offset);
            }
            int charCount = 0;
            Token lastToken = null;
            foreach (Token token in tokens)
            {
                lastToken = token;
                if (charCount + token.Text.Length > offset)
                {
                    int line, column;
                    CalculateLineColumnForOffset(token, offset - charCount, out line, out column);
                    return new PositionStruct(line, column, token.Begin.Offset + (offset - charCount), token.Filename);
                }
                charCount += token.Text.Length;
            }
            if (lastToken != null)
            {
                return new PositionStruct(lastToken.End.Line, lastToken.End.Column, lastToken.End.Offset, lastToken.Filename);
            }
            else
            {
                return new PositionStruct(1, 1, 0, string.Empty);
            }
        }

        internal static void CalculateLineColumnForOffset(Token token, int offset, out int line, out int column)
        {
            CalculateLineColumnForOffset(token.Text, offset, 0, token.Begin.Line, token.Begin.Column, out line, out column);
        }

        internal static void CalculateLineColumnForOffset(string text, int offset, 
            int offsetDelta, int lineDelta, int columnDelta, out int line, out int column)
        {
            line = lineDelta;
            column = columnDelta;
            int counter = offsetDelta;
            while (counter < offset)
            {
                bool newLineWithCR = false;

                if (text[counter] == '\r')
                {
                    newLineWithCR = true;
                }
                else if (text[counter] == '\n')
                {
                    line++;
                    column = 0;
                }
                counter++;
                if (newLineWithCR && counter < text.Length && text[counter] != '\n')
                {
                    line++;
                    column = 0;
                }
                column++;
            }
        }

        private int CalculateVarsUnresolvedOffset(int offset)
        {
            // find offset of the beginning of variable substitution (if offset points to the middle of it)
            int diff = 0;
            foreach (VariableReference reference in variableRefs)
            {
                if (reference.Start >= offset)
                {
                    break;
                }
                else if (reference.VariableValue != null && offset < reference.Start + reference.VariableValue.Length)
                {
                    offset = reference.Start;
                    break;
                }
                if (reference.VariableValue != null)
                {
                    diff += reference.Length - reference.VariableValue.Length;
                }
            }
            return offset + diff;
        }

    }
}
