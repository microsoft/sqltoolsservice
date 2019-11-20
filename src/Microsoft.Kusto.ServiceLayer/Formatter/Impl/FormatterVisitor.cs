//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Extensibility;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    /// <summary>
    /// The main entry point for our formatter implementation, via the <see cref="Format(string, FormatOptions, bool, Replacement.OnReplace)"/> method. 
    /// This converts a text string into a parsed AST using the Intellisense parser.
    /// It then uses the Visitor pattern to find each element in the tree and determine if any edits are needed based on 
    /// All edits are applied after the entire AST has been visited using an algorithm that keeps track of index changes caused by previous updates. This allows
    /// us to apply multiple edits to a text string in one sweep.
    /// 
    /// A note on the <see cref="SqlCodeObjectVisitor"/> implementation: All of the override nodes in the Intellisense AST are defined here, and routed to the Format method which looks up a matching
    /// formatter to handle them. Any entry not explicitly formatted will use the no-op formatter which passes through the text unchanged.
    /// </summary>
    internal partial class FormatterVisitor : SqlCodeObjectVisitor
    {
        private readonly IMultiServiceProvider serviceProvider;

        public FormatterVisitor(FormatContext context, IMultiServiceProvider serviceProvider)
            : base()
        {
            Context = context;
            this.serviceProvider = serviceProvider;
        }

        private void Format<T>(T codeObject) where T : SqlCodeObject
        {
            ASTNodeFormatter f = GetFormatter(codeObject);
            f.Format();
        }

        private ASTNodeFormatter GetFormatter<T>(T codeObject) where T:SqlCodeObject
        {
            Type astType = typeof(T);
            ASTNodeFormatter formatter;

            var formatterFactory = serviceProvider.GetServices<ASTNodeFormatterFactory>().FirstOrDefault(f => astType.Equals(f.SupportedNodeType));
            if (formatterFactory != null)
            {
                formatter = formatterFactory.Create(this, codeObject);
            }
            else
            {
                formatter = new NoOpFormatter(this, codeObject);
            }

            return formatter;
        }

        public FormatContext Context { get; private set; }

        public void VerifyFormat()
        {
            ParseResult result = Parser.Parse(Context.FormattedSql);
            SqlScript newScript = result.Script;
            VerifyTokenStreamsOnlyDifferByWhitespace(Context.Script, newScript);
        }

        internal static bool IsTokenWhitespaceOrComma(SqlScript script, int tokenIndex)
        {
            int tokenId = script.TokenManager.TokenList[tokenIndex].TokenId;
            return script.TokenManager.IsTokenWhitespace(tokenId) || (tokenId == FormatterTokens.TOKEN_COMMA);
        }

        internal static bool IsTokenWhitespaceOrComment(SqlScript script, int tokenIndex)
        {
            int tokenId = script.TokenManager.TokenList[tokenIndex].TokenId;

            return script.TokenManager.IsTokenWhitespace(tokenId) || script.TokenManager.IsTokenComment(tokenId);
        }
        
        /// <summary>
        /// Checks that the token streams of two SqlScript objects differ only by whitespace tokens or
        /// by the relative positioning of commas and comments. The important rule enforced is that there are
        /// no changes in relative positioning which involve tokens other than commas, comments or whitespaces.
        /// </summary>
        /// <param name="script1">SQL script containing the first token stream.</param>
        /// <param name="script2">SQL script containing the second token stream.</param>
        public static void VerifyTokenStreamsOnlyDifferByWhitespace(SqlScript script1, SqlScript script2)
        {
            // We break down the relative positioning problem into assuring that the token streams have identical ids
            // both when we ignore whitespaces and commas as well as when we ignore whitespaces and comments
            VerifyTokenStreamsOnlyDifferBy(script1, script2, IsTokenWhitespaceOrComma);
            VerifyTokenStreamsOnlyDifferBy(script1, script2, IsTokenWhitespaceOrComment);
        }

        internal delegate bool IgnoreToken(SqlScript script, int tokenIndex);

        public static void VerifyTokenStreamsOnlyDifferBy(SqlScript script1, SqlScript script2, IgnoreToken ignoreToken )
        {
            int t1 = 0;
            int t2 = 0;

            while (t1 < script1.TokenManager.Count && t2 < script2.TokenManager.Count)
            {
                // advance t1 until it is pointing at a non-whitespace token
                while (t1 < script1.TokenManager.Count && ignoreToken(script1, t1))
                {
                    ++t1;
                }

                // advance t2 until it is pointing at a non-whitespace token
                while (t2 < script2.TokenManager.Count && ignoreToken(script2, t2))
                {
                    ++t2;
                }

                if (t1 >= script1.TokenManager.Count || t2 >= script2.TokenManager.Count)
                {
                    break;
                }


                //
                // TODO:  need special logic here to deal with the placement of commas
                //

                // verify the tokens are equal
                if (script1.TokenManager.TokenList[t1].TokenId != script2.TokenManager.TokenList[t2].TokenId)
                {
                    string msg = "The comparison failed between tokens at {0} & {1}.  The token IDs were {2} and {3} respectively. Script1 = {4}.  Script2 = {5}";
                    msg = string.Format(CultureInfo.CurrentCulture, msg, t1, t2, script1.TokenManager.TokenList[t1].TokenId, script2.TokenManager.TokenList[t2].TokenId, script1.Sql, script2.Sql);
                    throw new FormatFailedException(msg);
                }

                ++t1;
                ++t2;
            }

            // one of the streams is exhausted, verify that the only tokens left in the other stream are whitespace tokens
            Debug.Assert(t1 >= script1.TokenManager.Count || t2 >= script2.TokenManager.Count, "expected to be at the end of one of the token's streams");
            int t = t1;
            SqlScript s = script1;
            if (t2 < script2.TokenManager.Count)
            {
                Debug.Assert(t1 >= script1.TokenManager.Count, "expected to be at end of script1's token stream");
                t = t2;
                s = script2;
            }

            while (t < s.TokenManager.Count)
            {
                if (!ignoreToken(s, t))
                {
                    string msg = "Unexpected non-whitespace token at index {0}, token ID {1}";
                    msg = string.Format(CultureInfo.CurrentCulture, msg, t, s.TokenManager.TokenList[t].TokenId);
                    throw new FormatFailedException(msg);
                }
            }
        }

        
        

    }

}
