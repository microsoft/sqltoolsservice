//------------------------------------------------------------------------------
// <copyright file="TextSpan.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal struct TextSpan
    {        
        public int iEndIndex;
        public int iEndLine;        
        public int iStartIndex;
        public int iStartLine;
    }
}
