//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public sealed class Token
    {
        /// <summary>
        /// Token class used by the lexer in Batch Parser
        /// </summary>
        internal Token(LexerTokenType tokenType, PositionStruct begin, PositionStruct end, string text, string filename)
        {
            TokenType = tokenType;
            Begin = begin;
            End = end;
            Text = text;
            Filename = filename;
        }

        /// <summary>
        /// Get file name associated with Token
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Get beginning position for the Token 
        /// </summary>
        public PositionStruct Begin { get; private set; }

        /// <summary>
        /// Get end position for the Token 
        /// </summary>
        public PositionStruct End { get; private set; }

        /// <summary>
        /// Get text assocaited with the Token 
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Get token type of the Token
        /// </summary>
        public LexerTokenType TokenType { get; private set; }
    }
}
