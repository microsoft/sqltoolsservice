//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System.Text;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    internal class TextBlock
    {
        private Parser _parser;
        private IEnumerable<Token> _tokens;

        public TextBlock(Parser parser, Token token) : this(parser, new[] { token })
        {
        }

        public TextBlock(Parser parser, IEnumerable<Token> tokens)
        {
            _parser = parser;
            _tokens = tokens;
        }

        public void GetText(bool resolveVariables, out string text, out LineInfo lineInfo)
        {
            StringBuilder sb = new StringBuilder();
            List<VariableReference> variableRefs = null;

            if (resolveVariables == false)
            {
                foreach (Token token in _tokens)
                {
                    sb.Append(token.Text);
                }
            }
            else
            {
                variableRefs = new List<VariableReference>();
                foreach (Token token in _tokens)
                {
                    if (token.TokenType == LexerTokenType.Text)
                    {
                        sb.Append(_parser.ResolveVariables(token, sb.Length, variableRefs));
                    }
                    else
                    {
                        // comments and whitespaces do not need variable expansion
                        sb.Append(token.Text);
                    }
                }
            }
            lineInfo = new LineInfo(_tokens, variableRefs);
            text = sb.ToString();
        }

    }
}
