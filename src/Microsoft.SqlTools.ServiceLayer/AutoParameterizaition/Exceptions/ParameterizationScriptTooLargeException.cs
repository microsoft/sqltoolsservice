//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition.Exceptions
{
    internal class ParameterizationScriptTooLargeException : ParameterizationParsingException
    {
        public readonly int ScriptLength;

        // LineNumber and ColumnNumber are defaulted to 1 because this exception is thrown if the script is very large and lineNumber and columnNumber dont make much sense
        public ParameterizationScriptTooLargeException(int scriptLength, string errorMessage) : base(lineNumber: 1, columnNumber: 1, errorMessage: errorMessage)
        {
            ScriptLength = scriptLength;
        }
    }
}
