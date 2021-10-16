//------------------------------------------------------------------------------
// <copyright file="SqlModelUpdaterUtils.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal static class SqlModelUpdaterUtils
    {
        public static void TraceAndThrow(string errorMessage)
        {
            SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, errorMessage);
            throw new SqlModelUpdaterException(errorMessage);
        }

        public static string GetBuiltInTypeName(SqlDataType sqlType)
        {
            if (sqlType == SqlDataType.Variant)
            {
                // in our model, the enum value cannot be correctly returned as the type name
                // so using the utils method on the engine's enum value
                return SqlInterpretationUtils.GetSqlBuiltInDataTypeName(SqlDataTypeOption.Sql_Variant);
            }
            else
            {
                return SqlInterpretationUtils.GetSqlBuiltInDataTypeName(sqlType);
            }
        }

        /// <summary>
        /// insert a comma for a script when necessary if the script is to be inserted into a list
        /// </summary>
        public static void InsertIntoCollection(
            ScriptFragmentGenerator scriptGenerator,
            int newItemIndex,
            int existingItemsCount,
            ScriptBuilderDelegate scriptBuilder,
            bool multiline = false,
            TSqlTokenType delimiter = TSqlTokenType.Comma,
            TSqlTokenType collectionLeftDelimiter = TSqlTokenType.LeftParenthesis,
            TSqlTokenType collectionRightDelimiter = TSqlTokenType.RightParenthesis)
        {
            if (existingItemsCount == 0)
            {
                // empty collection: add new item surrounded by collection delimiters
                scriptGenerator.AppendKeyword(collectionLeftDelimiter);
                if (multiline)
                {
                    scriptGenerator.AppendIndentationForNewItem();
                }
                scriptBuilder(scriptGenerator);
                if (multiline)
                {
                    scriptGenerator.AppendNewLine();
                }
                scriptGenerator.AppendKeyword(collectionRightDelimiter);
            }
            else if (existingItemsCount == newItemIndex && newItemIndex != 0)
            {
                // append to the end of the existing list: preppend a delimiter before the actual script
                scriptGenerator.AppendDelimiter(delimiter);
                if (multiline)
                {
                    scriptGenerator.AppendIndentationForNewItem();
                }
                scriptBuilder(scriptGenerator);
            }
            else
            {
                // for any other position: append a delimiter after the script
                scriptBuilder(scriptGenerator);
                scriptGenerator.AppendDelimiter(delimiter);
                if (multiline)
                {
                    scriptGenerator.AppendIndentationForNewItem();
                }
            }
        }

        public static bool IsSignificantToken(TSqlParserToken token)
        {
            return
                token.TokenType != TSqlTokenType.WhiteSpace &&
                token.TokenType != TSqlTokenType.SingleLineComment &&
                token.TokenType != TSqlTokenType.MultilineComment &&
                token.TokenType != TSqlTokenType.EndOfFile;
        }

        public static TAstType GetPrimaryAst<TAstType>(ISqlModelElement element) where TAstType : TSqlFragment
        {
            // Graph Columns don't appear in the AST.
            SqlSimpleColumn simpleColumn = element as SqlSimpleColumn;
            if (simpleColumn != null && simpleColumn.GraphType != SqlColumnGraphType.None)
            {
                return null;
            }

            return element.PrimarySource.ScriptDom as TAstType;
        }

        public static bool IsColumnDefinition(TSqlFragment fragment)
        {
            return fragment is ColumnDefinition;
        }

        public static bool IsConstraintDefinition(TSqlFragment fragment)
        {
            return fragment is ConstraintDefinition;
        }

        /// <summary>
        /// Calculate the length of a definition (column or constraint) to be used when deleting.
        /// </summary>
        /// <param name="fragment">The AST subtree of the statement.  Must be a ColumnDefinition or ConstraintDefinition;
        /// otherwise, an error value will be returned (-1)</param>
        /// <returns>Returns the last offset used for deleting the definition; on error, return -1</returns>
        public static int CalculateDefinitionLastTokenOffset(TSqlFragment fragment)
        {
            const int ErrorValue = -1;
            Debug.Assert(fragment != null);
            if (fragment is ColumnDefinition ||
                fragment is ConstraintDefinition)
            {
                return CalculateLastTokenOffset(fragment);
            }
            return ErrorValue;
        }

        internal static int CalculateLastTokenOffset(TSqlFragment fragment)
        {
            const int ErrorValue = -1;
            Debug.Assert(fragment != null);
            IList<TSqlParserToken> stream = fragment.ScriptTokenStream;
            Debug.Assert(stream != null && stream.Count > 0);

            int firstAfter = fragment.LastTokenIndex + 1;
            TSqlParserToken token;
            int tokenIndex;
            try
            {
                FindToken(
                    stream,
                    firstAfter,
                    tok => { return tok.TokenType == TSqlTokenType.Comma || tok.TokenType == TSqlTokenType.RightParenthesis; },
                    out tokenIndex,
                    out token);
            }
            catch (SqlModelUpdaterException)
            {
                return ErrorValue;
            }

            if (token == null || tokenIndex == ErrorValue)
            {
                return ErrorValue;
            }

            if (token.TokenType == TSqlTokenType.Comma)
            {
                // after the end of the comma token
                return token.Offset + token.Text.Length;
            }

            // for RParen, offset is up to next after last token of column definition statement
            return stream[firstAfter].Offset;
        }


        internal static void FindToken(
            IList<TSqlParserToken> tokenStream,
            int startIndex,
            Func<TSqlParserToken, bool> match,
            out int tokenIndex,
            out TSqlParserToken token)
        {
            FindToken(tokenStream, startIndex, match, t => true, out tokenIndex, out token);
        }

        /// <summary>
        /// Iterate the stoken stream to find the match token 
        /// until the continueCondition is not satisfied.
        /// </summary>
        internal static void FindToken(
            IList<TSqlParserToken> tokenStream,
            int startIndex,
            Func<TSqlParserToken, bool> match,
            Func<TSqlParserToken, bool> continueCondition,
            out int tokenIndex,
            out TSqlParserToken token)
        {
            tokenIndex = -1;
            token = null;

            for (int i = startIndex; i < tokenStream.Count; i++)
            {
                if (match(tokenStream[i]))
                {
                    tokenIndex = i;
                    token = tokenStream[i];
                    break;
                }
                if (!continueCondition(tokenStream[i]))
                {
                    break;
                }
            }
        }

        internal static void FindTokenBackward(
            IList<TSqlParserToken> tokenStream,
            int startIndex,
            Func<TSqlParserToken, bool> match,
            out int tokenIndex,
            out TSqlParserToken token)
        {
            tokenIndex = -1;
            token = null;

            for (int i = startIndex; i >= 0; i--)
            {
                if (match(tokenStream[i]))
                {
                    tokenIndex = i;
                    token = tokenStream[i];
                    break;
                }
            }
        }

        internal static bool CheckTokenSequence(IList<TSqlParserToken> tokenStream, int startIndex, int endIndex, out int firstFoundToken, out int lastFoundToken,
            params Func<TSqlParserToken, bool>[] tokens)
        {
            firstFoundToken = -1;
            lastFoundToken = -1;
            // At least one token has to be specified
            Debug.Assert(tokens.Length > 0);

            int current = startIndex;
            TSqlParserToken currentToken = tokenStream[current];
            int currentTokenTest = 0;

            do
            {
                // skip whitespaces/comments
                while (currentToken.TokenType == TSqlTokenType.WhiteSpace ||
                    currentToken.TokenType == TSqlTokenType.MultilineComment ||
                    currentToken.TokenType == TSqlTokenType.SingleLineComment)
                {
                    current++;
                    if (current > endIndex)
                    {
                        return false;
                    }
                    currentToken = tokenStream[current];
                }
                // check if it is searched token    
                if (!tokens[currentTokenTest](currentToken))
                {
                    return false;
                }
                if (currentTokenTest == 0)
                {
                    firstFoundToken = current;
                }
                if (currentTokenTest == tokens.Length - 1)
                {
                    lastFoundToken = current;
                    return true;
                }
                currentTokenTest++;
                current++;
                if (current > endIndex)
                {
                    return false;
                }
                currentToken = tokenStream[current];
            } while (true);
        }

        public static ScalarExpression GetExtendedPropertyValueAst(SqlExtendedProperty extendedProperty)
        {
            ExecuteStatement extPropAst = SqlModelUpdaterUtils.GetPrimaryAst<ExecuteStatement>(extendedProperty);

            ParameterResolver parameterResolver = new ParameterResolver(
                SqlInterpretationConstants.ParameterDefinitionForSpAddExtendedProperty,
                extPropAst.ExecuteSpecification.ExecutableEntity.Parameters,
                extendedProperty.Model.Comparer);
            ParameterValueInfo propertyValueParam = parameterResolver[SqlInterpretationConstants.AtValue];

            return propertyValueParam.ParameterAstNode.ParameterValue;
        }


        public static SqlScriptUpdateInfo AppendTextToFile(ISqlModelElement element, string text)
        {
            bool hasGo;
            TSqlParserToken lastToken;
            ISqlModelElement elementToCheck = element;

            if (element is SqlTable && (element as SqlTable).IsAutoGeneratedHistoryTable)
            {
                elementToCheck = (element as SqlTable).TemporalSystemVersioningCurrentTable;
            }

            CheckGoAndGetLastToken(elementToCheck, out hasGo, out lastToken);

            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
            sfGen.AppendNewLine();
            if (hasGo == false)
            {
                sfGen.AppendKeyword(TSqlTokenType.Go);
                sfGen.AppendNewLine();
            }
            sfGen.AppendText(text);
            text = sfGen.GetScriptFragment();

            SqlScriptUpdateInfo scriptUpdate = new SqlScriptUpdateInfo(elementToCheck.PrimarySource.SourceName);
            scriptUpdate.AddUpdate(
                lastToken.Offset,
                lastToken.Line,
                lastToken.Column,
                length: 0, // remove nothing
                newText: text);

            return scriptUpdate;
        }

        /// <summary>
        /// checks if the last significant token is GO and returns the last token (EOF)
        /// </summary>
        /// <param name="element"></param>
        /// <param name="hasGo"></param>
        /// <param name="lastToken"></param>
        public static void CheckGoAndGetLastToken(ISqlModelElement element, out bool hasGo, out TSqlParserToken lastToken)
        {
            hasGo = false;
            lastToken = null;

            SqlSchemaModel model = (SqlSchemaModel)element.Model; ;
            TSqlParser parser = model.GetParser(element);
            string script = model.ScriptCache.GetScript(element.PrimarySource.SourceName);
            IList<ParseError> errors;
            IList<TSqlParserToken> tokens = parser.GetTokenStream(new StringReader(script), out errors);

            lastToken = tokens[tokens.Count - 1];

            int tokenIndex;
            TSqlParserToken token;
            FindTokenBackward(tokens, tokens.Count - 1, t => IsSignificantToken(t), out tokenIndex, out token);
            hasGo = token.TokenType == TSqlTokenType.Go;
        }

        /// <summary>
        /// Check if the constraint definition is inline with the column definition.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="constraint"></param>
        /// <returns></returns>
        public static bool IsInlineColumnConstraint(SqlColumn column, SqlConstraint constraint)
        {
            // Graph columns don't appear in the AST.
            //
            if (column.PrimarySource != null &&
                string.Compare(column.PrimarySource.SourceName, constraint.PrimarySource.SourceName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                int columnOffsetStart = column.PrimarySource.Offset;
                int columnOffsetEnd = columnOffsetStart + column.PrimarySource.Length;

                return (constraint.PrimarySource.Offset > columnOffsetStart && constraint.PrimarySource.Offset < columnOffsetEnd);
            }
            return false;
        }

        public static bool DoesColumnHaveCheckOrForeignKeyOrNullableConstraint(SqlColumn sqlColumn)
        {
            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(sqlColumn);
            if (columnAst.Constraints != null)
            {
                foreach (ConstraintDefinition constraintAst in columnAst.Constraints)
                {
                    if ((constraintAst is ForeignKeyConstraintDefinition) ||
                        (constraintAst is CheckConstraintDefinition) ||
                        (constraintAst is NullableConstraintDefinition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool DoesIndexHaveFilterDefinition(SqlIndex sqlIndex)
        {
            if (sqlIndex.FilterPredicate != null && !String.IsNullOrWhiteSpace(sqlIndex.FilterPredicate.Script))
            {
                return true;
            }

            return false;
        }

        public static bool IsInlinePrimaryKeyColumn(SqlColumn column)
        {
            bool isPrimaryKeyColumn = false;

            SqlTable table = column.Parent as SqlTable;
            if (table != null &&
                table.Constraints != null &&
                table.Constraints.Count > 0)
            {
                foreach (SqlConstraint constraint in table.Constraints)
                {
                    SqlPrimaryKeyConstraint primaryKey = constraint as SqlPrimaryKeyConstraint;
                    if (primaryKey != null)
                    {
                        IList<SqlInlineConstraintAnnotation> annos = primaryKey.GetAnnotations<SqlInlineConstraintAnnotation>();
                        if (annos.Count > 0) // the primary key is inlined
                        {
                            foreach (SqlIndexedColumnSpecification columnSpec in primaryKey.ColumnSpecifications)
                            {
                                if (columnSpec.Column == column)
                                {
                                    isPrimaryKeyColumn = true;
                                    break;
                                }
                            }

                            if (isPrimaryKeyColumn)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return isPrimaryKeyColumn;
        }

        public static bool IsSpaceToken(TSqlParserToken token)
        {
            return
                token.TokenType == TSqlTokenType.WhiteSpace &&
                IsNewLineToken(token) == false;
        }

        public static bool IsNewLineToken(TSqlParserToken token)
        {
            return
                token.TokenType == TSqlTokenType.WhiteSpace &&
                (token.Text.StartsWith(SqlModelUpdaterConstants.NewLine, StringComparison.Ordinal) == true || // \n
                 token.Text.StartsWith(SqlModelUpdaterConstants.Return, StringComparison.Ordinal) == true);    // \r
        }

        public static void RemoveWhitespaceAroundSourcePosition(SqlScriptUpdateInfo info, TSqlParser parser, string fullScript, ISourceInformation sourceInfo)
        {
            RemovePrecedingWhitespace(info, parser, fullScript, sourceInfo);
            RemoveFollowingWhitespaceAndGo(info, parser, fullScript, sourceInfo);
        }

        private static void RemoveFollowingWhitespaceAndGo(SqlScriptUpdateInfo info, TSqlParser parser, string fullScript, ISourceInformation sourceInfo)
        {
            // Remove following whitespace and/or GO command
            using (StringReader sr = new StringReader(fullScript.Substring(sourceInfo.Offset + sourceInfo.Length)))
            {
                IList<ParseError> errors;

                // Get token stream for the script following element's script
                IList<TSqlParserToken> tokens = parser.GetTokenStream(sr, out errors);

                if (tokens == null || tokens.Count == 0)
                {
                    return;
                }

                int index = 0;

                while (tokens[index].TokenType == TSqlTokenType.WhiteSpace)
                {
                    index++;
                }

                if (tokens[index].TokenType == TSqlTokenType.Go ||
                    tokens[index].TokenType == TSqlTokenType.EndOfFile)
                {
                    TSqlParserToken lastToken = null;

                    if (tokens[index].TokenType == TSqlTokenType.Go)
                    {
                        index++;
                    }

                    if (tokens[index].TokenType == TSqlTokenType.EndOfFile)
                    {
                        lastToken = tokens[index];
                    }
                    else if (IsNewLineToken(tokens[index]))
                    {
                        lastToken = tokens[index + 1];
                    }

                    if (lastToken != null && lastToken.Offset > 0)
                    {
                        TSqlFragment fragment = sourceInfo.ScriptDom;
                        if (fragment != null)
                        {
                            TSqlParserToken firstToken = fragment.ScriptTokenStream[fragment.LastTokenIndex + 1];
                            info.AddUpdate(
                                firstToken.Offset,
                                firstToken.Line,
                                firstToken.Column,
                                lastToken.Offset, // length
                                newText: string.Empty);
                        }
                    }
                }
            }
        }

        private static void RemovePrecedingWhitespace(SqlScriptUpdateInfo info, TSqlParser parser, string fullScript, ISourceInformation sourceInfo)
        {
            // Remove following whitespace and/or GO command
            using (StringReader sr = new StringReader(fullScript.Substring(0, sourceInfo.Offset)))
            {
                IList<ParseError> errors;

                // Get token stream for the script following element's script
                IList<TSqlParserToken> tokens = parser.GetTokenStream(sr, out errors);
                if (tokens == null || tokens.Count <= 1)
                {
                    return;
                }

                // Remove preceding whitespace
                int currentIndex = tokens.Count - 2; // last token is EndOfFile token, start from second to last
                bool onlyWhitespace = true;
                while (currentIndex >= 0 && tokens[currentIndex].TokenType != TSqlTokenType.Go)
                {
                    if (tokens[currentIndex].TokenType != TSqlTokenType.WhiteSpace)
                    {
                        onlyWhitespace = false;
                        break;
                    }
                    currentIndex--;
                }

                if (onlyWhitespace)
                {
                    currentIndex++;
                    if (currentIndex > 0)
                    {
                        // If last token was GO, leave it and a newline
                        while (!IsNewLineToken(tokens[currentIndex]))
                        {
                            currentIndex++;
                        }
                        currentIndex++;
                    }

                    TSqlParserToken currentToken = tokens[currentIndex];
                    if (currentToken.Offset < sourceInfo.Offset)
                    {
                        info.AddUpdate(currentToken.Offset,
                            currentToken.Line,
                            currentToken.Column,
                            sourceInfo.Offset - currentToken.Offset,
                            string.Empty);
                    }

                }
            }
        }

        /// <summary>
        /// Return a list of extended property whose name is the inputed property name
        /// </summary>
        internal static IList<SqlExtendedProperty> GetExtendedPropertyList(ISqlExtendedPropertyHost propertyHost, string propertyName)
        {
            IList<SqlExtendedProperty> targetedExtendedProperties = new List<SqlExtendedProperty>();

            if (propertyHost != null && propertyHost.ExtendedProperties != null)
            {
                StringComparison compType = propertyHost.Model.Collation.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                foreach (SqlExtendedProperty prop in propertyHost.ExtendedProperties)
                {
                    DatabaseSchemaProvider dsp = ((DataSchemaModel)propertyHost.Model).DatabaseSchemaProvider;
                    UserInteractionServices services = dsp.UserInteractionServices;

                    if (services.GetElementName(prop, ElementNameStyle.SimpleName).Equals(propertyName, compType))
                    {
                        targetedExtendedProperties.Add(prop);
                    }
                }
            }

            return targetedExtendedProperties;
        }
    }
}
