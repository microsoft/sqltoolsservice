//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.AutoParameterizaition.Exceptions
{
    /// <summary>
    /// ParameterizationParsingException is used to surface parse errors encountered in the TSQL batch while creating a parse tree
    /// </summary>
    public class ParameterizationParsingException : Exception
    {
        public readonly int ColumnNumber;
        public readonly int LineNumber;

        public ParameterizationParsingException(int lineNumber, int columnNumber, string errorMessage) : base(errorMessage)
        {
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }
    }
}
