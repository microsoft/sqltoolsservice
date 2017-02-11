//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    internal sealed class BatchParserException : Exception
    {
        const string ErrorCodeName = "ErrorCode";
        const string BeginName = "Begin";
        const string EndName = "End";
        const string TextName = "Text";
        const string TokenTypeName = "TokenType";

        ErrorCode errorCode;
        PositionStruct begin;
        PositionStruct end;
        string text;
        LexerTokenType tokenType;

        /// <summary>
        /// Class for a custom exception for the Batch Parser
        /// </summary>
        public BatchParserException(ErrorCode errorCode, Token token, string message)
            : base(message)
        {
            this.errorCode = errorCode;
            begin = token.Begin;
            end = token.End;
            text = token.Text;
            tokenType = token.TokenType;
        }

        public ErrorCode ErrorCode { get { return errorCode; } }

        public PositionStruct Begin { get { return begin; } }

        public PositionStruct End { get { return end; } }

        public string Text { get { return text; } }

        public LexerTokenType TokenType { get { return tokenType; } }

    }
}
