//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
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
        
        internal virtual void ProcessChild(SqlCodeObject child)
        {
            Validate.IsNotNull(nameof(child), child);
            child.Accept(this.Visitor);
        }

        internal virtual void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            this.Visitor.Context.ProcessTokenRange(startTokenNumber, firstChildStartTokenNumber);
        }

        internal virtual void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            this.Visitor.Context.ProcessTokenRange(lastChildEndTokenNumber, endTokenNumber);
        }

        internal virtual void ProcessInterChildRegion(SqlCodeObject lastChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(lastChild), lastChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            int lastChildEnd = lastChild.Position.endTokenNumber;
            int nextChildStart = nextChild.Position.startTokenNumber;

            this.Visitor.Context.ProcessTokenRange(lastChildEnd, nextChildStart);
        }

        public override void Format()
        {
            LexLocation loc = ASTNodeFormatter.GetLexLocationForNode(this.CodeObject);

            SqlCodeObject firstChild = this.CodeObject.Children.FirstOrDefault();
            if (firstChild != null)
            {
                //
                // format the text from the start of the object to the start of it's first child
                //
                LexLocation firstChildStart = ASTNodeFormatter.GetLexLocationForNode(firstChild);
                this.ProcessPrefixRegion(loc.startTokenNumber, firstChildStart.startTokenNumber);

                //LexLocation lastChildLexLocation = null;
                SqlCodeObject previousChild = null;
                foreach (SqlCodeObject child in this.CodeObject.Children)
                {
                    //
                    // format text between the last child's end & current child's start
                    //
                    if (previousChild != null)
                    {
                        //this.ProcessInterChildRegion(lastChildLexLocation.endTokenNumber, childLexLocation.startTokenNumber);
                        this.ProcessInterChildRegion(previousChild, child);
                    }

                    //
                    //  format text of the the current child
                    //
                    this.ProcessChild(child);
                    previousChild = child;

                }

                //
                // format text from end of last child to end of object.
                //
                Debug.Assert(previousChild != null, "last child is null.  Need to write code to deal with this case");
                this.ProcessSuffixRegion(previousChild.Position.endTokenNumber, loc.endTokenNumber);
            }
            else
            {
                // no children
                this.Visitor.Context.ProcessTokenRange(loc.startTokenNumber, loc.endTokenNumber);
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
                Visitor.Context.ProcessTokenRange(currentToken, currentToken + 1);
            }
        }

        private void ProcessWhitepace(int currentToken, NormalizeWhitespace normalizeFunction, TokenData token)
        {
            string originalWhiteSpace = Visitor.Context.GetTokenRangeAsOriginalString(currentToken, currentToken + 1);
            if (HasPreviousToken(currentToken))
            {
                TokenData previousToken = PreviousTokenData(currentToken);
                if (previousToken.TokenId == FormatterTokens.LEX_END_OF_LINE_COMMENT)
                {
                    Debug.Assert(originalWhiteSpace.StartsWith("\n", StringComparison.OrdinalIgnoreCase), "unexpected start character for whitespace after LEX_END_OF_LINE_COMMENT token");
                    if (originalWhiteSpace.StartsWith("\n", StringComparison.OrdinalIgnoreCase)
                        && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Only include \r on Windows platforms
                        originalWhiteSpace = '\r' + originalWhiteSpace;
                    }
                }
            }

            string newWhiteSpace = normalizeFunction(originalWhiteSpace, this.Visitor.Context);

            AddReplacement(new Replacement(token.StartIndex, Visitor.Context.GetTokenRangeAsOriginalString(currentToken, currentToken + 1), newWhiteSpace));
        }

        private void ProcessEndOfLine(int currentToken, TokenData t)
        {
            //
            // the new line character is split over the LEX_END_OF_LINE_COMMENT token and a following whitespace token.
            // we deal with that here. 
            //
            string comment = Visitor.Context.GetTokenRangeAsOriginalString(currentToken, currentToken + 1);
#if DEBUG
            Debug.Assert(IsTokenWithIdWhitespace(currentToken + 1), "end-of-line comment wasn't followed by a whitespace");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Only expect \r on windows
                Debug.Assert(comment.EndsWith("\r", StringComparison.OrdinalIgnoreCase), "comment didn't end with \\r character");
            }
            string nextWhiteSpace = this.Visitor.Context.GetTokenRangeAsOriginalString(currentToken + 1, currentToken + 2);
            Debug.Assert(nextWhiteSpace.StartsWith("\n", StringComparison.OrdinalIgnoreCase), "whitespace token following end-of-line-commment didnt start with \\n character");
#endif
            if (comment.EndsWith("\r", StringComparison.OrdinalIgnoreCase))
            {
                AddReplacement(new Replacement(t.StartIndex, comment, comment.Substring(0, comment.Length - 1)));
            }
        }

        protected bool IsTokenWithIdWhitespace(int tokenId)
        {
            if (HasToken(tokenId))
            {
                var tokenManager = Visitor.Context.Script.TokenManager;
                return tokenManager.IsTokenWhitespace(tokenManager.TokenList[tokenId].TokenId);
            }
            return false;
        }

        protected bool IsTokenWhitespace(TokenData tokenData)
        {
            return Visitor.Context.Script.TokenManager.IsTokenWhitespace(tokenData.TokenId);
        }

        protected TokenData GetTokenData(int currentToken)
        {
            if (HasToken(currentToken))
            {
                return Visitor.Context.Script.TokenManager.TokenList[currentToken];
            }
            return default(TokenData);
        }

        private TokenData PreviousTokenData(int currentToken)
        {
            if (HasPreviousToken(currentToken))
            {
                return Visitor.Context.Script.TokenManager.TokenList[currentToken - 1];
            }
            return default(TokenData);
        }

        private TokenData NextTokenData(int currentToken)
        {
            if (HasToken(currentToken))
            {
                return Visitor.Context.Script.TokenManager.TokenList[currentToken + 1];
            }
            return default(TokenData);
        }

        protected bool HasPreviousToken(int currentToken)
        {
            return HasToken(currentToken - 1);
        }
        
        protected bool HasToken(int tokenIndex)
        {
            return tokenIndex >= 0 && tokenIndex < this.Visitor.Context.Script.TokenManager.TokenList.Count;
        }

        protected void AddReplacement(Replacement replacement)
        {
            Visitor.Context.Replacements.Add(replacement);
        }

        protected string GetIndentString()
        {
            return Visitor.Context.GetIndentString();
        }

        internal delegate string NormalizeWhitespace(string original, FormatContext context);
    }
}
