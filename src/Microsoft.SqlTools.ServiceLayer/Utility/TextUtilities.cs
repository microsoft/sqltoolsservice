//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.EditorServices.Utility
{
    public static class TextUtilities
    {
        /// <summary>
        /// Find the position of the previous delimeter for autocomplete token replacement.
        /// SQL Parser may have similar functionality in which case we'll delete this method.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRow"></param>
        /// <param name="startColumn"></param>
        /// <returns></returns>
        public static int PositionOfPrevDelimeter(string sql, int startRow, int startColumn)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return 1;
            }

            int prevLineColumns = 0;
            for (int i = 0; i < startRow; ++i)
            {
                while (sql[prevLineColumns] != '\n' && prevLineColumns < sql.Length)
                {
                    ++prevLineColumns;
                }
                ++prevLineColumns;
            }

            startColumn += prevLineColumns;

            if (startColumn - 1 < sql.Length)
            {
                while (--startColumn >= prevLineColumns)
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
            }

            return startColumn + 1 - prevLineColumns;
        }
    }
}
