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

        //public static int GetTokenText(strign sql, int startRow, int startColumn, )

        /// <summary>
        /// Find the position of the previous delimeter for autocomplete token replacement.
        /// SQL Parser may have similar functionality in which case we'll delete this method.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRow"></param>
        /// <param name="startColumn"></param>
        public static int PositionOfPrevDelimeter(string sql, int startRow, int startColumn)
        { 
            string tokenText;
            return PositionOfPrevDelimeter(sql, startRow, startColumn, out tokenText); 
        }

        /// <summary>
        /// Find the position of the previous delimeter for autocomplete token replacement.
        /// SQL Parser may have similar functionality in which case we'll delete this method.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRow"></param>
        /// <param name="startColumn"></param>
        /// <param name="tokenText"></param>
        public static int PositionOfPrevDelimeter(string sql, int startRow, int startColumn, out string tokenText)
        {            
            tokenText = null;                  

            int prevNewLine;
            int cursorPos = PositionOfCursor(sql, startRow, startColumn, out prevNewLine);
            int delimeterPos = cursorPos;
            
            startColumn = cursorPos;
            if (startColumn - 1 < sql.Length)
            {
                while (--startColumn >= prevNewLine)
                {
                    if (sql[startColumn] == ' ' 
                        || sql[startColumn] == '\t'
                        || sql[startColumn] == '\n'
                        || sql[startColumn] == '.'
                        || sql[startColumn] == '+'
                        || sql[startColumn] == '-'
                        || sql[startColumn] == '*'
                        || sql[startColumn] == '>'
                        || sql[startColumn] == '<'
                        || sql[startColumn] == '='
                        || sql[startColumn] == '/'
                        || sql[startColumn] == '%')
                    {
                        break;
                    }
                }

                delimeterPos =  startColumn + 1 - prevNewLine;
                tokenText = sql.Substring(delimeterPos, cursorPos - delimeterPos);
            }

            return delimeterPos;
        }
    }
}
