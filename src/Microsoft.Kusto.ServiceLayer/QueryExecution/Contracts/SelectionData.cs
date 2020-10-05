// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts
{
    /// <summary> 
    /// Container class for a selection range from file 
    /// </summary>
    /// TODO: Remove this in favor of buffer range end-to-end
    public class SelectionData
    {
        public SelectionData() { }

        public SelectionData(int startLine, int startColumn, int endLine, int endColumn)
        {
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        #region Properties

        public int EndColumn { get; set; }

        public int EndLine { get; set; }

        public int StartColumn { get; set; }
        public int StartLine { get; set; }

        #endregion

        public BufferRange ToBufferRange()
        {
            return new BufferRange(StartLine, StartColumn, EndLine, EndColumn);
        }

        public static SelectionData FromBufferRange(BufferRange range)
        {
            return new SelectionData
            {
                StartLine = range.Start.Line,
                StartColumn = range.Start.Column,
                EndLine = range.End.Line,
                EndColumn = range.End.Column
            };
        }
    }
}
