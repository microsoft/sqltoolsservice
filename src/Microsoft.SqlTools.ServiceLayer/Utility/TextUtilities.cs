//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public static class TextUtilities
    {       
        /// <summary>
        /// Find the position of the cursor in the SQL script content buffer and return previous new line position
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRow"></param>
        /// <param name="startColumn"></param>
        /// <param name="prevNewLine"></param>
        public static int PositionOfCursor(string sql, int startRow, int startColumn, out int prevNewLine)
        {
            prevNewLine = 0;
            if (string.IsNullOrWhiteSpace(sql))
            {
                return 1;
            }
            
            for (int i = 0; i < startRow; ++i)
            {
                while (prevNewLine < sql.Length && sql[prevNewLine] != '\n')
                {
                    ++prevNewLine;
                }
                ++prevNewLine;
            }

            return startColumn + prevNewLine;
        }

        /// <summary>
        /// Find the position of the previous delimeter for autocomplete token replacement.
        /// SQL Parser may have similar functionality in which case we'll delete this method.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRow"></param>
        /// <param name="startColumn"></param>
        /// <param name="tokenText"></param>
        public static int PositionOfPrevDelimeter(string sql, int startRow, int startColumn)
        {            
            int prevNewLine;
            int delimeterPos = PositionOfCursor(sql, startRow, startColumn, out prevNewLine);

            if (delimeterPos - 1 < sql.Length)
            {
                while (--delimeterPos >= prevNewLine)
                {
                    if (IsCharacterDelimeter(sql[delimeterPos]))
                    {
                        break;
                    }
                }

                delimeterPos = delimeterPos + 1 - prevNewLine;
            }

            return delimeterPos;
        }

        /// <summary>
        /// Find the position of the next delimeter for autocomplete token replacement.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRow"></param>
        /// <param name="startColumn"></param>
        public static int PositionOfNextDelimeter(string sql, int startRow, int startColumn)
        {            
            int prevNewLine;
            int delimeterPos = PositionOfCursor(sql, startRow, startColumn, out prevNewLine);
           
            while (delimeterPos < sql.Length)
            {
                if (IsCharacterDelimeter(sql[delimeterPos]))
                {
                    break;
                }
                ++delimeterPos;              
            }

            return delimeterPos - prevNewLine;
        }

        /// <summary>
        /// Determine if the character is a SQL token delimiter
        /// </summary>
        /// <param name="ch"></param>
        private static bool IsCharacterDelimeter(char ch)
        {
            return ch == ' ' 
                || ch == '\t'
                || ch == '\n'
                || ch == '.'
                || ch == '+'
                || ch == '-'
                || ch == '*'
                || ch == '>'
                || ch == '<'
                || ch == '='
                || ch == '/'
                || ch == '%'
                || ch == ','
                || ch == ';'
                || ch == '('
                || ch == ')';
        }

        public static string RemoveSquareBracketSyntax(string tokenText)
        {
            if(tokenText.StartsWith("[") && tokenText.EndsWith("]"))
            {
                return tokenText.Substring(1, tokenText.Length - 2);
            }
            return tokenText;
        }
    }
}
