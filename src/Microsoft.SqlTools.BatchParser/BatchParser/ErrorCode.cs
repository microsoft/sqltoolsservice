//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public enum ErrorCode
    {
        ErrorCodeBase = 0,

        Success = ErrorCodeBase,
        // Lexer error codes
        UnsupportedCommand = ErrorCodeBase + 1,
        UnrecognizedToken = ErrorCodeBase + 2,
        StringNotTerminated = ErrorCodeBase + 3,
        CommentNotTerminated = ErrorCodeBase + 4,

        // Parser error codes
        InvalidVariableName = ErrorCodeBase + 6,
        InvalidNumber = ErrorCodeBase + 7,
        TokenExpected = ErrorCodeBase + 8,
        Aborted = ErrorCodeBase + 9,
        CircularReference = ErrorCodeBase + 10,
        VariableNotDefined = ErrorCodeBase + 11,
    }
}
