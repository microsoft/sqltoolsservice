//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    internal class FormatContext
    {
        private ReplacementQueue replacements = new ReplacementQueue();
        private string formattedSql;

        internal FormatContext(SqlScript sqlScript, FormatOptions options)
        {
            FormatOptions = options;
            Script = sqlScript;
            LoadKeywordIdentifiers();
        }

        internal SqlScript Script { get; private set; }
        internal FormatOptions FormatOptions { get; set; }
        internal int IndentLevel { get; set; }
        internal HashSet<int> KeywordIdentifiers { get; set; }

        private void LoadKeywordIdentifiers()
        {
            KeywordIdentifiers = new HashSet<int>();

            List<int[]> keywordRanges = new List<int[]>()
            {
                new [] { Tokens.TOKEN_OR.ToInt(), Tokens.TOKEN_NOT.ToInt() },
                new [] { Tokens.TOKEN_ELSE.ToInt(), Tokens.TOKEN_UNPIVOT.ToInt() },
                new [] { Tokens.TOKEN_ALL.ToInt(), Tokens.TOKEN_PERCENT.ToInt() },
                // TODO is TOKEN_PROC_SEMI a keyword?
                new [] { Tokens.TOKEN_OPENQUERY.ToInt(), Tokens.TOKEN_FREETEXT.ToInt() },
                new [] { Tokens.TOKEN_c_MOVE.ToInt(), Tokens.TOKEN_c_ROLLUP.ToInt() },
                new [] { Tokens.TOKEN_REVERT.ToInt(), Tokens.TOKEN_c_OPENJSON.ToInt() },
                new [] { Tokens.TOKEN_s_CDA_TYPE.ToInt(), Tokens.TOKEN_s_CDA_POLICY.ToInt() },
                new [] { Tokens.TOKEN_BEGIN_CS.ToInt(), Tokens.TOKEN_END_CS.ToInt() }
                // Note: after this it becomes hard to interpret actual keywords in the Tokens enum.
                // Should review and re-assess later
            };
            
            foreach(int[] range in keywordRanges)
            {
                for(int i = range[0]; i <= range[1]; i++)
                {
                    KeywordIdentifiers.Add(i);
                }
            }
            KeywordIdentifiers.Add(FormatterTokens.LEX_BATCH_SEPERATOR);
        }

        public string FormattedSql
        {
            get
            {
                if (formattedSql == null)
                {
                    DoFormatSql();
                }
                return formattedSql;
            }
        }

        private void DoFormatSql()
        {
            StringBuilder code = new StringBuilder(Script.Sql);
            foreach (Replacement r in Replacements)
            {
                r.Apply((int position, int length, string formattedText) =>
                {
                    if (length > 0)
                    {
                        if (formattedText.Length > 0)
                        {
                            code.Remove(position, length);
                            code.Insert(position, formattedText);
                        }
                        else
                        {
                            code.Remove(position, length);
                        }
                    }
                    else
                    {
                        if (formattedText.Length > 0)
                        {
                            code.Insert(position, formattedText);
                        }
                        else
                        {
                            throw new FormatException(SR.ErrorEmptyStringReplacement);
                        }
                    }
                });
            }
            formattedSql = code.ToString();
        }

        public ReplacementQueue Replacements
        {
            get
            {
                return replacements;
            }
        }

        internal void IncrementIndentLevel()
        {
            IndentLevel++;
        }

        internal void DecrementIndentLevel()
        {
            if (IndentLevel == 0)
            {
                throw new FormatFailedException("can't decrement indent level.  It is already 0.");
            }
            IndentLevel--;
        }

        public string GetIndentString()
        {
            if (FormatOptions.UseTabs)
            {
                return new string('\t', IndentLevel);
            }
            else
            {
                return new string(' ', IndentLevel * FormatOptions.SpacesPerIndent);
            }
        }

        internal string GetTokenRangeAsOriginalString(int startTokenNumber, int endTokenNumber)
        {
            string sql = string.Empty;
            if (endTokenNumber > startTokenNumber && startTokenNumber > -1 && endTokenNumber > -1)
            {
                sql = Script.TokenManager.GetText(startTokenNumber, endTokenNumber);
            }
            return sql;
        }

        /// <summary>
        /// Will apply any token-level formatting (e.g., uppercase/lowercase of keywords).
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        internal void ProcessTokenRange(int startTokenNumber, int endTokenNumber)
        {

            for (int i = startTokenNumber; i < endTokenNumber; i++)
            {
                string sql = GetTokenRangeAsOriginalString(i, i + 1);

                if (IsKeywordToken(Script.TokenManager.TokenList[i].TokenId))
                {
                    if (FormatOptions.UppercaseKeywords)
                    {
                        TokenData tok = Script.TokenManager.TokenList[i];
                        Replacements.Add(new Replacement(tok.StartIndex, sql, sql.ToUpperInvariant()));
                    }
                    else if (FormatOptions.LowercaseKeywords)
                    {
                        TokenData tok = Script.TokenManager.TokenList[i];
                        Replacements.Add(new Replacement(tok.StartIndex, sql, sql.ToLowerInvariant()));
                    }
                }
            }

        }

        internal void AppendTokenRangeAsString(int startTokenNumber, int endTokenNumber)
        {
            ProcessTokenRange(startTokenNumber, endTokenNumber);
        }

        private bool IsKeywordToken(int tokenId)
        {
            return KeywordIdentifiers.Contains(tokenId);
        }

        internal List<PaddedSpaceSeparatedListFormatter.ColumnSpacingFormatDefinition> CurrentColumnSpacingFormatDefinitions { get; set; }

    }

}
