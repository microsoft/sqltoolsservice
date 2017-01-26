//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Text;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    internal class TextBlock
    {
        private Parser parser;
        private IEnumerable<Token> tokens;

        /// <summary>
        /// Constructor for the TextBlock class 
        /// </summary>
        public TextBlock(Parser parser, Token token) : this(parser, new[] { token })
        {
        }

        /// <summary>
        /// Constructor for the TextBlock class 
        /// </summary>
        public TextBlock(Parser parser, IEnumerable<Token> tokens)
        {
            this.parser = parser;
            this.tokens = tokens;
        }

        /// <summary>
        /// Get text from TextBlock
        /// </summary>
        public void GetText(bool resolveVariables, out string text, out LineInfo lineInfo)
        {
            StringBuilder sb = new StringBuilder();
            List<VariableReference> variableRefs = null;

            if (resolveVariables == false)
            {
                foreach (Token token in tokens)
                {
                    sb.Append(token.Text);
                }
            }
            else
            {
                variableRefs = new List<VariableReference>();
                foreach (Token token in tokens)
                {
                    if (token.TokenType == LexerTokenType.Text)
                    {
                        sb.Append(parser.ResolveVariables(token, sb.Length, variableRefs));
                    }
                    else
                    {
                        // comments and whitespaces do not need variable expansion
                        sb.Append(token.Text);
                    }
                }
            }
            lineInfo = new LineInfo(tokens, variableRefs);
            text = sb.ToString();
        }

    }
}
