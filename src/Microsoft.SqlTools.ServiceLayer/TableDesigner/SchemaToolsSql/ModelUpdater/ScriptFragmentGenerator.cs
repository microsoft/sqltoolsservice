//------------------------------------------------------------------------------
// <copyright file="ScriptFragmentGenerator.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Tools.Schema.ScriptDom.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using CodeGenerationSupporter = Microsoft.Data.Tools.Schema.ScriptDom.Sql.CodeGenerationSupporter;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal delegate void ScriptBuilderDelegate(ScriptFragmentGenerator fragmentGenerator);
    
    internal sealed class ScriptFragmentGenerator
    {
        private static Dictionary<TSqlTokenType, string> KeywordNames = new Dictionary<TSqlTokenType, string>()
        {
            // TODO: ask for this dictionary from the engine parser, or complete this list
            { TSqlTokenType.As, SqlModelUpdaterConstants.AS },
            { TSqlTokenType.Asc, SqlModelUpdaterConstants.ASC },
            { TSqlTokenType.Begin, SqlModelUpdaterConstants.BEGIN },
            { TSqlTokenType.Check, SqlModelUpdaterConstants.CHECK },
            { TSqlTokenType.Clustered, SqlModelUpdaterConstants.CLUSTERED },
            { TSqlTokenType.Constraint, SqlModelUpdaterConstants.CONSTRAINT },
            { TSqlTokenType.Create, SqlModelUpdaterConstants.CREATE },
            { TSqlTokenType.Default, CodeGenerationSupporter.Default },
            { TSqlTokenType.Delete, SqlModelUpdaterConstants.DELETE },
            { TSqlTokenType.Desc, SqlModelUpdaterConstants.DESC },
            { TSqlTokenType.End, SqlModelUpdaterConstants.END },
            { TSqlTokenType.Exec, SqlModelUpdaterConstants.EXEC },
            { TSqlTokenType.For, SqlModelUpdaterConstants.FOR},
            { TSqlTokenType.Foreign, SqlModelUpdaterConstants.FOREIGN },
            { TSqlTokenType.Go, SqlModelUpdaterConstants.GO },
            { TSqlTokenType.Identity, CodeGenerationSupporter.Identity },
            { TSqlTokenType.Index, SqlModelUpdaterConstants.INDEX },
            { TSqlTokenType.Insert, SqlModelUpdaterConstants.INSERT },
            { TSqlTokenType.Integer, CodeGenerationSupporter.Int },
            { TSqlTokenType.Key, CodeGenerationSupporter.Key },
            { TSqlTokenType.LeftParenthesis, CodeGenerationSupporter.LeftParenthesis },
            { TSqlTokenType.Not, SqlModelUpdaterConstants.NOT },
            { TSqlTokenType.NonClustered, SqlModelUpdaterConstants.NONCLUSTERED },
            { TSqlTokenType.Null, SqlModelUpdaterConstants.NULL },
            { TSqlTokenType.On, SqlModelUpdaterConstants.ON },
            { TSqlTokenType.Primary, SqlModelUpdaterConstants.PRIMARY },
            { TSqlTokenType.References, SqlModelUpdaterConstants.REFERENCES },
            { TSqlTokenType.RightParenthesis, CodeGenerationSupporter.RightParenthesis },
            { TSqlTokenType.Set, SqlModelUpdaterConstants.SET },
            { TSqlTokenType.Trigger, CodeGenerationSupporter.Trigger },
            { TSqlTokenType.Unique, SqlModelUpdaterConstants.UNIQUE },
            { TSqlTokenType.Update, SqlModelUpdaterConstants.UPDATE },
            { TSqlTokenType.With, SqlModelUpdaterConstants.WITH },

            { TSqlTokenType.Comma, CodeGenerationSupporter.Comma },
            { TSqlTokenType.EqualsSign, CodeGenerationSupporter.Equal },
        };

        private StringBuilder _stringBuilderForScript;
        private SqlScriptGeneratorOptions _defaultScriptGeneratorOptions = new SqlScriptGeneratorOptions();

        public ScriptFragmentGenerator()
        {
            _stringBuilderForScript = new StringBuilder();
        }

        public void AppendSpace(int repeat = 1)
        {
            for (int i = 0; i < repeat; ++i)
            {
                _stringBuilderForScript.Append(SqlModelUpdaterConstants.Space);
            }
        }

        public void AppendNewLine()
        {
            _stringBuilderForScript.AppendLine();
        }

        public void AppendIndentationForNewItem()
        {
            AppendNewLine();
            AppendSpace(_defaultScriptGeneratorOptions.IndentationSize);
        }

        /// <summary>
        /// Append a keyword such as CREATE, TABLE, or NULL
        /// </summary>
        /// <param name="tokenType"></param>
        public void AppendKeyword(TSqlTokenType tokenType)
        {
            _stringBuilderForScript.Append(GenerateKeyword(tokenType));
        }

        /// <summary>
        /// Append a collection delimiter
        /// </summary>
        /// <param name="tokenType"></param>
        public void AppendDelimiter(TSqlTokenType tokenType)
        {
            AppendKeyword(tokenType);
            AppendSpace();
        }

        /// <summary>
        /// Append a contextual keyword such as MAX, CONTENT, or DOCUMENT
        /// </summary>
        /// <param name="word"></param>
        public void AppendContextualKeyword(string word)
        {
            _stringBuilderForScript.Append(GenerateKeyword(word));
        }

        public void AppendIdentifier(string value, bool encode = true)
        {
            if (encode)
            {
                value = Identifier.EncodeIdentifier(value);
            }
            _stringBuilderForScript.Append(value);
        }

        public void AppendToken(TSqlParserToken token)
        {
            _stringBuilderForScript.Append(token.Text);
        }

        public void AppendText(string text)
        {
            _stringBuilderForScript.Append(text);
        }

        public string GetScriptFragment()
        {
            return _stringBuilderForScript.ToString();
        }

        /// <summary>
        /// Utility method to return the text for a keyword based on its token type
        /// having a central place to generate keywords will facilitate universal process such as changing casing in the future
        /// </summary>
        /// <param name="tokenType"></param>
        /// <returns></returns>
        public static string GenerateKeyword(TSqlTokenType tokenType)
        {
            return KeywordNames[tokenType];
        }

        /// <summary>
        /// Utility method to return the text for a keyword based on its text
        /// currently the keyword text is returned as is 
        /// having a central place to generate keywords will facilitate universal process such as changing casing in the future
        /// </summary>
        /// <param name="tokenType"></param>
        /// <returns></returns>
        public static string GenerateKeyword(string word)
        {
            return word;
        }
    }
}
