//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    internal abstract class ASTNodeFormatterT<T> : ASTNodeFormatter where T : SqlCodeObject
    {
        protected FormatterVisitor Visitor { get; private set; }
        protected T CodeObject { get; private set; }

        public ASTNodeFormatterT(FormatterVisitor visitor, T codeObject)
        {
            Validate.IsNotNull(nameof(visitor), visitor);
            Validate.IsNotNull(nameof(codeObject), codeObject);

            Visitor = visitor;
            CodeObject = codeObject;
        }
        
        protected TokenManager TokenManager
        {
            get { return Visitor.Context.Script.TokenManager; }
        }

        protected FormatOptions FormatOptions
        {
            get { return Visitor.Context.FormatOptions; }
        }

        internal virtual void ProcessChild(SqlCodeObject child)
        {
            Validate.IsNotNull(nameof(child), child);
            child.Accept(Visitor);
        }

        protected void IncrementIndentLevel()
        {
            Visitor.Context.IncrementIndentLevel();
        }

        protected void DecrementIndentLevel()
        {
            Visitor.Context.DecrementIndentLevel();
        }

        protected void ProcessTokenRange(int startTokenNumber, int endTokenNumber)
        {
            Visitor.Context.ProcessTokenRange(startTokenNumber, endTokenNumber);
        }

        protected void ProcessTokenRangeEnsuringOneNewLineMinumum(int startindex, int endIndex)
        {
            ProcessAndNormalizeWhitespaceRange(startindex, endIndex, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
        }

        protected void ProcessAndNormalizeWhitespaceRange(int startindex, int endIndex, NormalizeWhitespace normalizer)
        {
            ProcessAndNormalizeTokenRange(startindex, endIndex, normalizer, true);
        }


        protected void ProcessAndNormalizeTokenRange(int startindex, int endIndex, 
            NormalizeWhitespace normalizer, bool areAllTokensWhitespace)
        {
            for (int i = startindex; i < endIndex; i++)
            {
                ProcessTokenAndNormalize(i, normalizer, areAllTokensWhitespace);
            }
        }

        protected void ProcessTokenAndNormalize(int tokenIndex, NormalizeWhitespace normalizeFunction, bool areAllTokensWhitespace = true)
        {
            TokenData iTokenData = GetTokenData(tokenIndex);

            if (areAllTokensWhitespace)
            {
                DebugAssertTokenIsWhitespaceOrComment(iTokenData, tokenIndex);
            }
            normalizeFunction = normalizeFunction ?? FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
            SimpleProcessToken(tokenIndex, normalizeFunction);
        }

        protected void DebugAssertTokenIsWhitespaceOrComment(TokenData td, int tokenIndex)
        {
            Debug.Assert(TokenManager.IsTokenComment(td.TokenId) || IsTokenWhitespace(td), string.Format(CultureInfo.CurrentCulture,
                "Unexpected token \"{0}\", expected whitespace or comment.", GetTextForCurrentToken(tokenIndex))
            );
        }

        /// <summary>
        /// Logical aliases for ProcessTokenRange that indicates the starting region is to be analyzed
        /// </summary>
        internal virtual void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            ProcessTokenRange(startTokenNumber, firstChildStartTokenNumber);
        }

        /// <summary>
        /// Logical aliases for ProcessTokenRange that indicates the end region is to be analyzed
        /// </summary>
        internal virtual void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            ProcessTokenRange(lastChildEndTokenNumber, endTokenNumber);
        }

        internal virtual void ProcessInterChildRegion(SqlCodeObject lastChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(lastChild), lastChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            int lastChildEnd = lastChild.Position.endTokenNumber;
            int nextChildStart = nextChild.Position.startTokenNumber;

            ProcessTokenRange(lastChildEnd, nextChildStart);
        }

        public override void Format()
        {
            LexLocation loc = GetLexLocationForNode(CodeObject);

            SqlCodeObject firstChild = CodeObject.Children.FirstOrDefault();
            if (firstChild != null)
            {
                //
                // format the text from the start of the object to the start of it's first child
                //
                LexLocation firstChildStart = GetLexLocationForNode(firstChild);
                ProcessPrefixRegion(loc.startTokenNumber, firstChildStart.startTokenNumber);

                //LexLocation lastChildLexLocation = null;
                SqlCodeObject previousChild = null;
                foreach (SqlCodeObject child in CodeObject.Children)
                {
                    //
                    // format text between the last child's end & current child's start
                    //
                    if (previousChild != null)
                    {
                        //ProcessInterChildRegion(lastChildLexLocation.endTokenNumber, childLexLocation.startTokenNumber);
                        ProcessInterChildRegion(previousChild, child);
                    }

                    //
                    //  format text of the the current child
                    //
                    ProcessChild(child);
                    previousChild = child;

                }

                //
                // format text from end of last child to end of object.
                //
                Debug.Assert(previousChild != null, "last child is null.  Need to write code to deal with this case");
                ProcessSuffixRegion(previousChild.Position.endTokenNumber, loc.endTokenNumber);
            }
            else
            {
                // no children
                ProcessTokenRange(loc.startTokenNumber, loc.endTokenNumber);
            }
        }

        protected void SimpleProcessToken(int currentToken, NormalizeWhitespace normalizeFunction)
        {
            TokenData t = GetTokenData(currentToken);
            if (IsTokenWhitespace(t))
            {
                ProcessWhitepace(currentToken, normalizeFunction, t);
            }
            else if (t.TokenId == FormatterTokens.LEX_END_OF_LINE_COMMENT)
            {
                ProcessEndOfLine(currentToken, t);
            }
            else 
            {
                ProcessTokenRange(currentToken, currentToken + 1);
            }
        }

        private void ProcessWhitepace(int currentToken, NormalizeWhitespace normalizeFunction, TokenData token)
        {
            string originalWhiteSpace = GetTextForCurrentToken(currentToken);
            if (HasPreviousToken(currentToken))
            {
                TokenData previousToken = PreviousTokenData(currentToken);
                if (previousToken.TokenId == FormatterTokens.LEX_END_OF_LINE_COMMENT)
                {
                    if (originalWhiteSpace.StartsWith("\n", StringComparison.OrdinalIgnoreCase)
                        && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Replace \n with \r\n on Windows platforms
                        originalWhiteSpace = Environment.NewLine + originalWhiteSpace.Substring(1);
                    }
                }
            }

            string newWhiteSpace = normalizeFunction(originalWhiteSpace, Visitor.Context);

            AddReplacement(new Replacement(token.StartIndex, GetTextForCurrentToken(currentToken), newWhiteSpace));
        }

        protected string GetTextForCurrentToken(int currentToken)
        {
            return Visitor.Context.GetTokenRangeAsOriginalString(currentToken, currentToken + 1);
        }

        protected string GetTokenRangeAsOriginalString(int startTokenNumber, int endTokenNumber)
        {
            return Visitor.Context.GetTokenRangeAsOriginalString(startTokenNumber, endTokenNumber);
        }

        private void ProcessEndOfLine(int currentToken, TokenData t)
        {
            //
            // the new line character is split over the LEX_END_OF_LINE_COMMENT token and a following whitespace token.
            // we deal with that here. 
            //
            string comment = GetTextForCurrentToken(currentToken);
            if (comment.EndsWith("\r", StringComparison.OrdinalIgnoreCase))
            {
                AddReplacement(new Replacement(t.StartIndex, comment, comment.Substring(0, comment.Length - 1)));
            }
        }

        protected bool IsTokenWithIdWhitespace(int tokenId)
        {
            if (HasToken(tokenId))
            {
                return TokenManager.IsTokenWhitespace(TokenManager.TokenList[tokenId].TokenId);
            }
            return false;
        }

        protected bool IsTokenWhitespace(TokenData tokenData)
        {
            return TokenManager.IsTokenWhitespace(tokenData.TokenId);
        }


        protected TokenData GetTokenData(int currentToken)
        {
            if (HasToken(currentToken))
            {
                return TokenManager.TokenList[currentToken];
            }
            return default(TokenData);
        }

        protected TokenData PreviousTokenData(int currentToken)
        {
            if (HasPreviousToken(currentToken))
            {
                return TokenManager.TokenList[currentToken - 1];
            }
            return default(TokenData);
        }

        protected TokenData NextTokenData(int currentToken)
        {
            if (HasToken(currentToken))
            {
                return TokenManager.TokenList[currentToken + 1];
            }
            return default(TokenData);
        }

        protected bool HasPreviousToken(int currentToken)
        {
            return HasToken(currentToken - 1);
        }
        
        protected bool HasToken(int tokenIndex)
        {
            return tokenIndex >= 0 && tokenIndex < TokenManager.TokenList.Count;
        }

        protected void AddReplacement(Replacement replacement)
        {
            Visitor.Context.Replacements.Add(replacement);
        }

        protected void AddReplacement(int startIndex, string oldValue, string newValue)
        {
            AddReplacement(new Replacement(startIndex, oldValue, newValue));
        }

        protected void AddIndentedNewLineReplacement(int startIndex)
        {
            AddReplacement(new Replacement(startIndex, string.Empty, Environment.NewLine + Visitor.Context.GetIndentString()));
        }

        protected string GetIndentString()
        {
            return Visitor.Context.GetIndentString();
        }
        
        /// <summary>
        /// Finds an expected token 
        /// </summary>
        /// <param name="currentIndex">Current index to start the search at</param>
        /// <param name="id">ID defining the type of token being looked for - e.g. parenthesis, INSERT</param>
        protected int FindTokenWithId(int currentIndex, int id)
        {
            TokenData td = GetTokenData(currentIndex);
            while (td.TokenId != id && currentIndex < CodeObject.Position.endTokenNumber)
            {
                DebugAssertTokenIsWhitespaceOrComment(td, currentIndex);
                ++currentIndex;
                td = GetTokenData(currentIndex);
            }
            Debug.Assert(currentIndex < CodeObject.Position.endTokenNumber, "No token with ID" + id + " found in the columns definition.");
            return currentIndex;
        }
        
        internal delegate string NormalizeWhitespace(string original, FormatContext context);
    }
}
