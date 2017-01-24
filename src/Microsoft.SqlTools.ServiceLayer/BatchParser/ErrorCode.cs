//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    enum ErrorCode
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
