//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition
{
    public class CodeSenseItem
    {
        public readonly string Message;
        public readonly int StartRow;
        public readonly int EndRow;
        public readonly int StartCol;
        public readonly int EndCol;
        public readonly CodeSenseItemType Type;

        public CodeSenseItem(string message, int startRow, int startCol, int endRow, int endCol, CodeSenseItemType type)
        {
            Message = message;
            StartRow = startRow;
            EndRow = endRow < startRow ? startRow : endRow;
            StartCol = startCol;
            EndCol = endRow == startRow && endCol < startCol ? startCol : endCol;
            Type = type;
        }

        public enum CodeSenseItemType
        {
            Message,
            Error
        }
    }
}
