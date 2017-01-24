// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    public class BatchDefinition
    {
        public int startLine;
        public int endLine;
        public int startColumn;
        public int endColumn;
        public string batchText;

        public BatchDefinition(string batchText, int startLine, int endLine, int startColumn, int endColumn)
        {
            this.batchText = batchText;
            this.startLine = startLine;
            this.endLine = endLine;
            this.startColumn = startColumn;
            this.endColumn = endColumn;
        }
        
    }
}
