//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

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

        ErrorCode _errorCode;
        PositionStruct _begin;
        PositionStruct _end;
        string _text;
        LexerTokenType _tokenType;

        public BatchParserException(ErrorCode errorCode, Token token, string message)
            : base(message)
        {
            _errorCode = errorCode;
            _begin = token.Begin;
            _end = token.End;
            _text = token.Text;
            _tokenType = token.TokenType;
        }

        //leaving for incase we add serializable back again

        //private BatchParserException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //    _errorCode = (ErrorCode) info.GetInt32(ErrorCodeName);
        //    _begin = (PositionStruct) info.GetValue(BeginName, typeof(PositionStruct));
        //    _end = (PositionStruct) info.GetValue(EndName, typeof(PositionStruct));
        //    _text = info.GetString(TextName);
        //    _tokenType = (LexerTokenType)info.GetInt32(TokenTypeName);
        //}


        public ErrorCode ErrorCode { get { return _errorCode; } }

        public PositionStruct Begin { get { return _begin; } }

        public PositionStruct End { get { return _end; } }

        public string Text { get { return _text; } }

        public LexerTokenType TokenType { get { return _tokenType; } }



        //leaving for incase we add serializable back again

        //public override void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    base.GetObjectData(info, context);

        //    info.AddValue(ErrorCodeName, (int) _errorCode);
        //    info.AddValue(BeginName, _begin);
        //    info.AddValue(EndName, _end);
        //    info.AddValue(TextName, _text);
        //    info.AddValue(TokenTypeName, (int)_tokenType);
        //}

    }
}
